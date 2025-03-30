using Microsoft.AspNetCore.Mvc;
using WeatherThreading.Services;
using WeatherThreading.Models;

namespace WeatherThreading.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("historical")]
    public async Task<IActionResult> GetHistoricalWeather(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var weatherData = await _weatherService.GetHistoricalWeatherDataAsync(
                latitude, longitude, startDate, endDate);
            return Ok(weatherData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("processed")]
    public async Task<IActionResult> GetProcessedWeatherData([FromBody] WeatherDataRequest request)
    {
        try
        {
            var weatherData = await _weatherService.GetProcessedWeatherDataAsync(request);
            return Ok(weatherData);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 