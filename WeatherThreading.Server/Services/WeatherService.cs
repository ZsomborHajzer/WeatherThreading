using System.Text.Json;
using WeatherThreading.Models;
using System.Diagnostics;

namespace WeatherThreading.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://archive-api.open-meteo.com/v1/archive";
    private readonly WeatherContext _context;
    private readonly SemaphoreSlim _semaphore;
    private readonly IServiceProvider _serviceProvider;

    public WeatherService(HttpClient httpClient, WeatherContext context, IServiceProvider serviceProvider)
    {
        _httpClient = httpClient;
        _context = context;
        _serviceProvider = serviceProvider;
        //Limited to 2 becuase any more would cause the requests to be sent to quickly and we would get rate limited
        _semaphore = new SemaphoreSlim(2);
    }

    public async Task<WeatherData> GetHistoricalWeatherDataAsync(string location, DateTime startDate, DateTime endDate)
    {
        /*
        This is just a practice endpoint for testing the external api
        */
        var coords = ParameterMappings.CityMapping[location];
        double latitude = coords.Item1;
        double longitude = coords.Item2;
        var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&daily=temperature_2m_max";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var weatherData = JsonSerializer.Deserialize<WeatherData>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return weatherData ?? throw new Exception("Failed to deserialize weather data");
    }

    public async Task<WeatherDataGraphResponse> GetProcessedWeatherDataAsync(WeatherDataRequest request)
    {
        try
        {

            var apiParameters = request.Parameters
                .Select(p => ParameterMappings.RequestDataMapping.GetValueOrDefault(p, p))
                .ToList();

            var timeChunks = TimeRangeTools.SplitTimeRange(request.StartDate, request.EndDate);

            var coords = ParameterMappings.CityMapping[request.Location];
            double latitude = coords.Item1;
            double longitude = coords.Item2;

            try
            {
                if (await TimeRangeTools.AreDatesContinuousAsync(request, _context))
                {
                    var result = await GetWeatherDataFromDB(request);
                    var databaseResult = DataProcessor.FormatWeatherData(result, request);
                    databaseResult.Latitude = latitude;
                    databaseResult.Longitude = longitude;
                    return databaseResult;
                }
            }
            catch (Exception ex)
            {
            }

            var tasks = timeChunks.Select(async chunk =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await FetchWeatherDataForTimeRange(
                        latitude,
                        longitude,
                        chunk.Start,
                        chunk.End,
                        apiParameters
                    );
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);

            var mergedData = MergeWeatherDataResults(results);

            SaveDataInBackground(mergedData, request);

            mergedData.Daily.Remove("temperature_2m_max");
            mergedData.Daily.Remove("temperature_2m_min");

            var graphResult = DataProcessor.FormatWeatherData(mergedData.Daily, request);
            graphResult.Latitude = latitude;
            graphResult.Longitude = longitude;
            return graphResult;

        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }

    private async Task<WeatherData> FetchWeatherDataForTimeRange(
        double latitude,
        double longitude,
        DateTime startDate,
        DateTime endDate,
        List<string> parameters)
    {
        var parametersString = string.Join(",", parameters);
        var url = $"{BaseUrl}?latitude={latitude}&longitude={longitude}&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}&daily={parametersString}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API request failed: {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var weatherData = JsonSerializer.Deserialize<WeatherData>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return weatherData ?? throw new Exception("Failed to deserialize weather data");
    }

    private WeatherDataResponse MergeWeatherDataResults(WeatherData[] results)
    {
        var mergedResponse = DataProcessor.MergeResults(results);

        return mergedResponse;
    }

    private async Task<WeatherDataResponse> SaveWeatherDataToDB(WeatherDataResponse weatherData, WeatherDataRequest request, DBHandler dbHandler)
    {
        var coords = ParameterMappings.CityMapping[request.Location];
        var location = await dbHandler.GetOrCreateLocationAsync(request.Location, coords.Item1, coords.Item2);

        try
        {
            if (weatherData.Daily != null)
            {
                if (weatherData.Daily.ContainsKey("Date"))
                {
                    var dateList = weatherData.Daily["Date"].Cast<string>().ToList();

                    if (weatherData.Daily.ContainsKey("temperature_2m_max"))
                    {
                        await dbHandler.AddTemperatureBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_sum"))
                    {
                        await dbHandler.AddPrecipitationSumBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("wind_speed_10m_max"))
                    {
                        await dbHandler.AddWindBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("shortwave_radiation_sum"))
                    {
                        await dbHandler.AddRadiationBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_hours"))
                    {
                        await dbHandler.AddPrecipitationHoursBulk(weatherData, dateList, location);
                    }
                }
            }

            return weatherData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error orccured {ex}");
            throw;
        }
    }

    private async Task<Dictionary<string, List<object>>> GetWeatherDataFromDB(WeatherDataRequest request)
    {
        var dbHandler = new DBHandler(_context);

        var parameterKey = request.Parameters.FirstOrDefault();

        var queryResults = await dbHandler.GetWeatherDataDynamic(request, parameterKey);

        if (queryResults.Count == 0)
        {
            return new Dictionary<string, List<object>>();
        }

        var requestedColumns = ParameterMappings.RequestColumnsMapping[parameterKey];

        var properties = queryResults.First().GetType().GetProperties()
            .Where(p => requestedColumns.Contains(p.Name))
            .ToList();

        var result = new Dictionary<string, List<object>>();

        foreach (var prop in properties)
        {
            result[prop.Name] = [.. queryResults.Select(d => prop.GetValue(d))];
        }

        return result;
    }

    private void SaveDataInBackground(WeatherDataResponse mergedData, WeatherDataRequest request)
    {
        /*
        A seperate Task gets created for database insertion with its own scope so that the response 
        can return without having the database finish inserting
        */

        _ = Task.Run(async () =>
           {
               try
               {
                   using (var scope = _serviceProvider.CreateScope())
                   {
                       var context = scope.ServiceProvider.GetRequiredService<WeatherContext>();
                       var dbHandler = new DBHandler(context);

                       await SaveWeatherDataToDB(mergedData, request, dbHandler);
                   }
               }
               catch (Exception ex)
               {
                   Console.WriteLine($"Error saving weather data to DB: {ex}");
               }
           });
    }

}
