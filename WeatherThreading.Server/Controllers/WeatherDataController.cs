using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;
using WeatherThreading.Server;

namespace WeatherThreading.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeatherDataController : ControllerBase
    {
        private readonly WeatherContext _context;

        public WeatherDataController(WeatherContext context)
        {
            _context = context;
        }

        // GET: api/WeatherData
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeatherData>>> GetWeatherDatas()
        {
            return await _context.WeatherDatas.ToListAsync();
        }

        // GET: api/WeatherData/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WeatherData>> GetWeatherData(int id)
        {
            var weatherData = await _context.WeatherDatas.FindAsync(id);

            if (weatherData == null)
            {
                return NotFound();
            }

            return weatherData;
        }

        // PUT: api/WeatherData/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWeatherData(int id, WeatherData weatherData)
        {
            if (id != weatherData.Id)
            {
                return BadRequest();
            }

            _context.Entry(weatherData).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WeatherDataExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/WeatherData
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<WeatherData>> PostWeatherData(WeatherData weatherData)
        {
            _context.WeatherDatas.Add(weatherData);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWeatherData", new { id = weatherData.Id }, weatherData);
        }

        // DELETE: api/WeatherData/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeatherData(int id)
        {
            var weatherData = await _context.WeatherDatas.FindAsync(id);
            if (weatherData == null)
            {
                return NotFound();
            }

            _context.WeatherDatas.Remove(weatherData);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WeatherDataExists(int id)
        {
            return _context.WeatherDatas.Any(e => e.Id == id);
        }
    }
}
