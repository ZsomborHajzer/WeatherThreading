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