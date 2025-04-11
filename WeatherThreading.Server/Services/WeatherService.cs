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
                    var databaseResult = FormatWeatherData(result, request);
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

            var graphResult = FormatWeatherData(mergedData.Daily, request);
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
        if (results.Length == 0)
            throw new Exception("No weather data results to merge");

        var mergedResponse = new WeatherDataResponse
        {
            Latitude = results[0].Latitude,
            Longitude = results[0].Longitude,
            Daily = new Dictionary<string, List<object>>()
        };

        //! We could also add threading here to speed up the merging process (think read/write lock or plinq)
        foreach (var result in results)
        {
            if (!mergedResponse.Daily.ContainsKey("Date"))
            {
                mergedResponse.Daily["Date"] = new List<object>();
            }
            mergedResponse.Daily["Date"].AddRange(result.Daily.Time.Select(t => (object)t));

            // Merge temperature data
            if (result.Daily.Temperature2mMax.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("temperature_2m_max"))
                {
                    mergedResponse.Daily["temperature_2m_max"] = new List<object>();
                }
                mergedResponse.Daily["temperature_2m_max"].AddRange(result.Daily.Temperature2mMax.Select(t => (object)t));
            }

            if (result.Daily.Temperature2mMin.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("temperature_2m_min"))
                {
                    mergedResponse.Daily["temperature_2m_min"] = new List<object>();
                }
                mergedResponse.Daily["temperature_2m_min"].AddRange(result.Daily.Temperature2mMin.Select(t => (object)t));
            }

            // Merge humidity data
            if (result.Daily.RelativeHumidity2m.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("relative_humidity_2m"))
                {
                    mergedResponse.Daily["relative_humidity_2m"] = new List<object>();
                }
                mergedResponse.Daily["relative_humidity_2m"].AddRange(result.Daily.RelativeHumidity2m.Select(h => (object)h));
            }

            // Merge precipitation data
            if (result.Daily.PrecipitationSum.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("precipitation_sum"))
                {
                    mergedResponse.Daily["precipitation_sum"] = new List<object>();
                }
                mergedResponse.Daily["precipitation_sum"].AddRange(result.Daily.PrecipitationSum.Select(p => (object)p));
            }

            if (result.Daily.PrecipitationHours.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("precipitation_hours"))
                {
                    mergedResponse.Daily["precipitation_hours"] = new List<object>();
                }
                mergedResponse.Daily["precipitation_hours"].AddRange(result.Daily.PrecipitationHours.Select(p => (object)p));
            }

            // Merge wind speed data
            if (result.Daily.WindSpeed.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("wind_speed_10m_max"))
                {
                    mergedResponse.Daily["wind_speed_10m_max"] = new List<object>();
                }
                mergedResponse.Daily["wind_speed_10m_max"].AddRange(result.Daily.WindSpeed.Select(w => (object)w));
            }

            // Merge radiation data
            if (result.Daily.ShortWaveRadiationSum.Any())
            {
                if (!mergedResponse.Daily.ContainsKey("shortwave_radiation_sum"))
                {
                    mergedResponse.Daily["shortwave_radiation_sum"] = new List<object>();
                }
                mergedResponse.Daily["shortwave_radiation_sum"].AddRange(result.Daily.ShortWaveRadiationSum.Select(s => (object)s));
            }
        }

        if (mergedResponse.Daily.ContainsKey("temperature_2m_max") && mergedResponse.Daily.ContainsKey("temperature_2m_min"))
        {
            var maxTemps = mergedResponse.Daily["temperature_2m_max"].Cast<double>().ToList();
            var minTemps = mergedResponse.Daily["temperature_2m_min"].Cast<double>().ToList();


            //calculating the average temperature using PLINQ and max processor core count
            mergedResponse.Daily["temperature_2m_avg"] = maxTemps
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select((max, index) => (max + minTemps[index]) / 2.0)
                .Cast<object>()
                .ToList();
        }

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

    private WeatherDataGraphResponse FormatWeatherData(Dictionary<string, List<object>> result, WeatherDataRequest request)
    {
        if (!result.ContainsKey("Date") || result.Count != 2)
        {
            throw new ArgumentException("Input dictionary must contain 'Date' and one value axis.");
        }

        var valueKey = result.Keys.First(k => k != "Date");

        var dateValuePairs = result["Date"]
            .Select((x, i) => new
            {
                Date = DateTime.Parse(x.ToString()),
                Value = Convert.ToDouble(result[valueKey][i])
            })
            .Where(pair => pair.Date >= request.StartDate && pair.Date <= request.EndDate)
            .ToList();

        if (!dateValuePairs.Any())
        {
            throw new InvalidOperationException("No data points found in the specified date range.");
        }

        var chartData = dateValuePairs
            .Select(pair => new ChartDataPoint
            {
                xaxis = pair.Date,
                yaxis = pair.Value
            })
            .ToList();

        return new WeatherDataGraphResponse
        {
            XAxisTitle = "Date",
            YAxisTitle = valueKey,
            Daily = new Dictionary<string, List<ChartDataPoint>>
            {
                {"data", chartData}
            }
        };
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
