using System.Text.Json;
using WeatherThreading.Models;


namespace WeatherThreading.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://archive-api.open-meteo.com/v1/archive";
    private readonly ILogger<WeatherService> _logger;
    private readonly WeatherContext _context;
    private readonly DBHandler _dbHandler;
    
    private readonly SemaphoreSlim _semaphore;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, WeatherContext context, DBHandler dbHandler)
    {
        _httpClient = httpClient;
        _logger = logger;
        _context = context;
        _dbHandler = dbHandler;
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount -1 > 0 ? Environment.ProcessorCount - 1 : 1, Environment.ProcessorCount -1 > 0 ? Environment.ProcessorCount - 1 : 1); 
    }

    public async Task<WeatherData> GetHistoricalWeatherDataAsync(string location, DateTime startDate, DateTime endDate)
    {

        var coords = Dictionaries.CityMapping[location];
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

    public async Task<WeatherDataResponse> GetProcessedWeatherDataAsync(WeatherDataRequest request)
    {
        try
        {
            var apiParameters = request.Parameters
                .Select(p => Dictionaries.ParameterMapping.GetValueOrDefault(p, p))
                .ToList();

            var timeChunks = TimeRangeSplitter.SplitTimeRange(request.StartDate, request.EndDate);

            var coords = Dictionaries.CityMapping[request.Location];
            double latitude = coords.Item1;
            double longitude = coords.Item2;

            
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

            await SaveWeatherDataToDB(mergedData, request);

            return mergedData;  

            // getDataFromDB
            // formateResponsefunc
           // return formatedResponse;        


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weather data request");
            throw;
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

        _logger.LogInformation("Fetching weather data for range: {StartDate} to {EndDate} with parameters: {Parameters}",
            startDate, endDate, parametersString);

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("API request failed with status {StatusCode}: {ErrorContent}",
                response.StatusCode, errorContent);
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
            Timezone = results[0].Timezone,
            Daily = new Dictionary<string, List<object>>()
        };

        //! We could also add threading here to speed up the merging process (think read/write lock or plinq)
        foreach (var result in results)
        {
            if (!mergedResponse.Daily.ContainsKey("time"))
            {
                mergedResponse.Daily["time"] = new List<object>();
            }
            mergedResponse.Daily["time"].AddRange(result.Daily.Time.Select(t => (object)t));

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

        return mergedResponse;
    }

    // private async Task<WeatherDataResponse> FindMissingDataFromDB(WeatherDataRequest request)
    // {
    //     // Check if the data is already in the database
    //     // If not, fetch it from the API
    //     // This is a placeholder for the actual database check logic

    //     return null;
    // }

    private async Task<WeatherDataResponse> SaveWeatherDataToDB(WeatherDataResponse weatherData, WeatherDataRequest request)
    {
        var coords = Dictionaries.CityMapping[request.Location];
        var location = await _dbHandler.GetOrCreateLocationAsync(request.Location, coords.Item1, coords.Item2);

        var DBHandler = new DBHandler(_context);

        try
        {

            if (weatherData.Daily != null)
            {
                if (weatherData.Daily.ContainsKey("time"))
                {
                    var timeList = weatherData.Daily["time"].Cast<string>().ToList();

                    if (weatherData.Daily.ContainsKey("temperature_2m_max"))
                    {
                        await DBHandler.AddTemperatureBulk(weatherData, timeList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_sum"))
                    {
                        await DBHandler.AddPrecipitationSumBulk(weatherData, timeList, location);
                    }

                    if (weatherData.Daily.ContainsKey("wind_speed_10m_max"))
                    {
                        await DBHandler.AddWindBulk(weatherData, timeList, location);
                    }

                    if (weatherData.Daily.ContainsKey("shortwave_radiation_sum"))
                    {
                        await DBHandler.AddRadiationBulk(weatherData, timeList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_hours"))
                    {
                        await DBHandler.AddPrecipitationHoursBulk(weatherData, timeList, location);
                    }
                }
            }

            return weatherData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving weather data to database");
            throw;
        }
    }

    private async Task<WeatherDataResponse> GetWeatherDataFromDB(WeatherDataRequest request)
    {
        //! THIS COULD BE DONE WITH LINQ
        // Retrieve the weather data from the database
        // This is a placeholder for the actual database retrieval logic
        // You would typically use Entity Framework or another ORM to retrieve the data

        // Example:
        // return await _dbContext.WeatherData
        //     .Where(w => w.Location == request.Location && w.StartDate >= request.StartDate && w.EndDate <= request.EndDate)
        //     .ToListAsync();

        return null;
    }

    private async Task<WeatherDataResponse> FormatWeatherDataForFrontend(WeatherDataResponse weatherData)
    {
        // Format the weather data for the frontend
        // This is a placeholder for the actual formatting logic

        // Example:
        // weatherData.FormattedData = weatherData.Daily.Temperature2mMax.Select(t => $"{t} °C").ToList();

        return weatherData;
    }

    // Temperature
    // 1950-1964, 1985-1990, 2000-2005, 2010-2015
}
