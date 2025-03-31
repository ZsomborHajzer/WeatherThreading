using System.Text.Json;
using WeatherThreading.Models;

namespace WeatherThreading.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://archive-api.open-meteo.com/v1/archive";
    private readonly ILogger<WeatherService> _logger;

    // Map frontend parameter names to API parameter names
    private static readonly Dictionary<string, string> ParameterMapping = new()
    {
        { "temperature_2m_max", "temperature_2m_max" },
        { "temperature_2m_min", "temperature_2m_min" },
        { "relative_humidity_2m", "relative_humidity_2m_mean" }
    };

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<WeatherData> GetHistoricalWeatherDataAsync(double latitude, double longitude, DateTime startDate, DateTime endDate)
    {
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
            // Map the parameters to their API equivalents
            var apiParameters = request.Parameters
                .Select(p => ParameterMapping.GetValueOrDefault(p, p))
                .ToList();

            // Split the time range into smaller chunks
            var timeChunks = TimeRangeSplitter.SplitTimeRange(request.StartDate, request.EndDate);
            
            // Create tasks for each time chunk
            //! Maybe semaphore here to limit the number of concurrent requests
            var tasks = timeChunks.Select(chunk => 
                FetchWeatherDataForTimeRange(
                    request.Latitude,
                    request.Longitude,
                    chunk.Start,
                    chunk.End,
                    apiParameters
                )
            );

            // Execute all tasks in parallel
            var results = await Task.WhenAll(tasks);

            // Merge the results
            return MergeWeatherDataResults(results);
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
        // Merge all results
        foreach (var result in results)
        {
            // Merge time data
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
        }

        return mergedResponse;
    }

    private async Task SaveWeatherDataToDatabase(WeatherDataResponse weatherData)
    {
        //! We could save the data to the database here
    }

    private async Task<WeatherDataResponse> GetWeatherDataFromDatabase(WeatherDataRequest request)
    {
        //! We could get the data from the database here
    }

    private async Task<WeatherDataResponse> FormatWeatherData(WeatherDataResponse weatherData)
    {
        //! We could format the data here
    }
} 