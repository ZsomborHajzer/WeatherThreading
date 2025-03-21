using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WeatherThreading.Models;

namespace WeatherThreading.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrecipitationController : ControllerBase
    {
        private readonly WeatherContext _context;

        public PrecipitationController(WeatherContext context)
        {
            _context = context;
        }

        // GET: api/Precipitation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Precipitation>>> GetPrecipitation()
        {
            return await _context.Precipitation.ToListAsync();
        }

        // GET: api/Precipitation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Precipitation>> GetPrecipitation(long id)
        {
            var precipitation = await _context.Precipitation.FindAsync(id);

            if (precipitation == null)
            {
                return NotFound();
            }

            return precipitation;
        }

        // PUT: api/Precipitation/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPrecipitation(long id, Precipitation precipitation)
        {
            if (id != precipitation.Id)
            {
                return BadRequest();
            }

            _context.Entry(precipitation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PrecipitationExists(id))
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

        // POST: api/Precipitation
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Precipitation>> PostPrecipitation(Precipitation precipitation)
        {
            _context.Precipitation.Add(precipitation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPrecipitation", new { id = precipitation.Id }, precipitation);
        }

        // DELETE: api/Precipitation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrecipitation(long id)
        {
            var precipitation = await _context.Precipitation.FindAsync(id);
            if (precipitation == null)
            {
                return NotFound();
            }

            _context.Precipitation.Remove(precipitation);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PrecipitationExists(long id)
        {
            return _context.Precipitation.Any(e => e.Id == id);
        }
    }
}
