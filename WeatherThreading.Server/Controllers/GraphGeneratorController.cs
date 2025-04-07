namespace WeatherThreading.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GraphGenerationController : ControllerBase
    {
        private readonly YourDbContext _context;

        public GraphGenerationController(WeatherContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetData(
            [FromQuery] string city,
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string yAxis)
        {
            try
            {
                // 1. Validate inputs
                if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(from) || 
                    string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(yAxis))
                {
                    return BadRequest("All parameters are required");
                }

                // 2. Parse dates
                if (!DateTime.TryParse(from, out var fromDate) || !DateTime.TryParse(to, out var toDate))
                {
                    return BadRequest("Invalid date format. Use YYYY-MM-DD");
                }

                // 3. Find location (flexible matching)
                var location = await _context.Locations
                    .FirstOrDefaultAsync(l => l.Name.Contains(city.Split(',')[0].Trim()));
                
                if (location == null)
                {
                    return NotFound($"City '{city}' not found");
                }

                // 4. Get data based on yAxis type
                object data = await GetWeatherData(yAxis, location.Id, fromDate, toDate);
                
                if (data == null)
                {
                    return BadRequest($"Unsupported measurement type: {yAxis}");
                }

                // 5. Return in exact required format
                return Ok(new 
                {
                    xaxistitle = "date",
                    yaxistitle = yAxis,
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<object> GetWeatherData(string yAxis, int locationId, DateTime fromDate, DateTime toDate)
        {
            return yAxis switch
            {
                "temperature" => await _context.Temperatures
                    .Where(t => t.LocationId == locationId && t.Date >= fromDate && t.Date <= toDate)
                    .OrderBy(t => t.Date)
                    .Select(t => new { xaxis = t.Date.ToString("yyyy-MM-dd"), yaxis = t.Value })
                    .ToListAsync(),
                    
                "relative_humidity" => await _context.Humidity
                    .Where(h => h.LocationId == locationId && h.Date >= fromDate && h.Date <= toDate)
                    .OrderBy(h => h.Date)
                    .Select(h => new { xaxis = h.Date.ToString("yyyy-MM-dd"), yaxis = h.Value })
                    .ToListAsync(),
                    
                // Add other cases similarly...
                    
                _ => null
            };
        }
    }
}
