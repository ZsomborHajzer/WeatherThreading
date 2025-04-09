using System.Text.Json;
using WeatherThreading.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.AspNetCore.Http.Timeouts;

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
        _semaphore = new SemaphoreSlim(2);
    }

    public async Task<WeatherData> GetHistoricalWeatherDataAsync(string location, DateTime startDate, DateTime endDate)
    {

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

            var timeChunks = TimeRangeSplitter.SplitTimeRange(request.StartDate, request.EndDate);

            var coords = ParameterMappings.CityMapping[request.Location];
            double latitude = coords.Item1;
            double longitude = coords.Item2;

            try
            {
                if (await AreDatesContinuousAsync(request))
                {
                    var result = await GetWeatherDataFromDB(request);
                    var databaseResult = FormatWeatherDataForFrontend(result);
                    databaseResult.Latitude = latitude;
                    databaseResult.Longitude = longitude;
                    return databaseResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Data not found in DB, proceeding to fetch from API");
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

            await SaveWeatherDataToDB(mergedData, request);

            mergedData.Daily.Remove("temperature_2m_max");
            mergedData.Daily.Remove("temperature_2m_min");

            var graphResult = FormatWeatherDataForFrontend(mergedData.Daily);
            graphResult.Latitude = latitude;
            graphResult.Longitude = longitude;
            return graphResult;    

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing weather data request");
            throw;
        }
    }

    private async Task<bool> AreDatesContinuousAsync(WeatherDataRequest request)
    {
        var expectedDates = Enumerable.Range(0, (request.EndDate - request.StartDate).Days + 1)
                                    .Select(offset => request.StartDate.Date.AddDays(offset))
                                    .ToHashSet();

        IEnumerable<DateTime> existingDates;

        var parameterKey = request.Parameters.FirstOrDefault();

        if (string.IsNullOrEmpty(parameterKey) || !ParameterMappings.TableNameMapping.ContainsKey(parameterKey))
        {
            throw new ArgumentException("Invalid or missing parameter in request.");
        }

        var locationObject = await _context.Location
            .FirstOrDefaultAsync(l => l.LocationName == request.Location);

        if (locationObject == null)
        {
            throw new ArgumentException($"Location '{request.Location}' not found.");
        }

        var locationId = locationObject.Id;

        var tableName = ParameterMappings.TableNameMapping[parameterKey];

        switch (tableName)
        {
            case "Temperature":
                existingDates = await _context.Temperature
                    .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                    .Select(x => x.Date.Date)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Precipitation":
                existingDates = await _context.Precipitation
                    .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                    .Select(x => x.Date.Date)
                    .Distinct()
                    .ToListAsync();
                break;

            case "PrecipitationHours":
                existingDates = await _context.PrecipitationHours
                    .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                    .Select(x => x.Date.Date)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Radiation":
                existingDates = await _context.Radiation
                    .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                    .Select(x => x.Date.Date)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Wind":
                existingDates = await _context.Wind
                    .Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate)
                    .Select(x => x.Date.Date)
                    .Distinct()
                    .ToListAsync();
                break;

            default:
                throw new ArgumentException($"Invalid table name: {tableName}");
        }

        var existingDateSet = existingDates.ToHashSet();

        return expectedDates.SetEquals(existingDateSet);
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
            mergedResponse.Daily["temperature_2m_avg"] = new List<object>();
            mergedResponse.Daily["temperature_2m_avg"] = mergedResponse.Daily["temperature_2m_max"]
                .Zip(mergedResponse.Daily["temperature_2m_min"], (max, min) => ((double)max + (double)min) / 2)
                .Cast<object>()
                .ToList();
        }

        return mergedResponse;
    }

    private async Task<WeatherDataResponse> SaveWeatherDataToDB(WeatherDataResponse weatherData, WeatherDataRequest request)
    {
        var coords = ParameterMappings.CityMapping[request.Location];
        var location = await _dbHandler.GetOrCreateLocationAsync(request.Location, coords.Item1, coords.Item2);

        var DBHandler = new DBHandler(_context);

        try
        {

            if (weatherData.Daily != null)
            {
                if (weatherData.Daily.ContainsKey("Date"))
                {
                    var dateList = weatherData.Daily["Date"].Cast<string>().ToList();

                    if (weatherData.Daily.ContainsKey("temperature_2m_max"))
                    {
                        await DBHandler.AddTemperatureBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_sum"))
                    {
                        await DBHandler.AddPrecipitationSumBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("wind_speed_10m_max"))
                    {
                        await DBHandler.AddWindBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("shortwave_radiation_sum"))
                    {
                        await DBHandler.AddRadiationBulk(weatherData, dateList, location);
                    }

                    if (weatherData.Daily.ContainsKey("precipitation_hours"))
                    {
                        await DBHandler.AddPrecipitationHoursBulk(weatherData, dateList, location);
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

    private async Task<Dictionary<string, List<object>>> GetWeatherDataFromDB(WeatherDataRequest request)
    {
        var parameterKey = request.Parameters.FirstOrDefault();

        if (string.IsNullOrEmpty(parameterKey) || !ParameterMappings.TableNameMapping.ContainsKey(parameterKey))
        {
            throw new ArgumentException("Invalid or missing parameter in request.");
        }

        var tableName = ParameterMappings.TableNameMapping[parameterKey];
        
        Console.WriteLine($"Fetching data from table: {tableName}");

        var locationObject = await _context.Location
            .FirstOrDefaultAsync(l => l.LocationName == request.Location);

        if (locationObject == null)
        {
            throw new ArgumentException($"Location '{request.Location}' not found.");
        }

        var locationId = locationObject.Id;

        var tableMap = new Dictionary<string, IQueryable<object>>
        {
            { "Temperature", _context.Temperature.Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate) },
            { "Precipitation", _context.Precipitation.Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate) },
            { "PrecipitationHours", _context.PrecipitationHours.Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate) },
            { "Wind", _context.Wind.Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate) },
            { "Radiation", _context.Radiation.Where(x => x.LocationId == locationId && x.Date >= request.StartDate && x.Date <= request.EndDate) }
        };

        if (!tableMap.ContainsKey(tableName))
        {
            throw new ArgumentException($"Invalid table name: {tableName}");
        }

        var queryResults = await tableMap[tableName].ToListAsync();

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

    private WeatherDataGraphResponse FormatWeatherDataForFrontend(Dictionary<string, List<object>> result)
    {
        if (!result.ContainsKey("Date") || result.Count != 2)
        {
            throw new ArgumentException("Input dictionary must contain 'Date' and one value axis.");
        }

        var dates = result["Date"].Select(x => DateTime.Parse(x.ToString())).ToList();
        var valueKey = result.Keys.First(k => k != "Date");
        var values = result[valueKey].Select(Convert.ToDouble).ToList();

        if (dates.Count != values.Count)
        {
            throw new InvalidOperationException("Date and value lists must have the same length.");
        }

        var chartData = dates.Select((date, i) => new ChartDataPoint
        {
                xaxis = date,
                yaxis = values[i]
            }).ToList();

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
}
