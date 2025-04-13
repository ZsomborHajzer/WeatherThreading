using WeatherThreading.Models;

namespace WeatherThreading.Services;

public interface IWeatherService
{
    Task<WeatherData> GetHistoricalWeatherDataAsync(string location, DateTime startDate, DateTime endDate);
    Task<WeatherDataGraphResponse> GetProcessedWeatherDataAsync(WeatherDataRequest request);
} 