using WeatherThreading.Models;

namespace WeatherThreading.Services;

public interface IWeatherService
{
    Task<WeatherData> GetHistoricalWeatherDataAsync(double latitude, double longitude, DateTime startDate, DateTime endDate);
    Task<WeatherDataResponse> GetProcessedWeatherDataAsync(WeatherDataRequest request);
} 