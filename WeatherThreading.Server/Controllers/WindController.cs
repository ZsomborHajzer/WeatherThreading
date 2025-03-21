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
    public class WindController : ControllerBase
    {
        private readonly WeatherContext _context;

        public WindController(WeatherContext context)
        {
            _context = context;
        }

        // GET: api/Wind
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Wind>>> GetWind_1()
        {
            return await _context.Wind.ToListAsync();
        }

        // GET: api/Wind/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Wind>> GetWind(long id)
        {
            var wind = await _context.Wind.FindAsync(id);

            if (wind == null)
            {
                return NotFound();
            }

            return wind;
        }

        // PUT: api/Wind/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWind(long id, Wind wind)
        {
            if (id != wind.Id)
            {
                return BadRequest();
            }

            _context.Entry(wind).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WindExists(id))
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

        // POST: api/Wind
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Wind>> PostWind(Wind wind)
        {
            _context.Wind.Add(wind);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWind", new { id = wind.Id }, wind);
        }

        // DELETE: api/Wind/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWind(long id)
        {
            var wind = await _context.Wind.FindAsync(id);
            if (wind == null)
            {
                return NotFound();
            }

            _context.Wind.Remove(wind);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WindExists(long id)
        {
            return _context.Wind.Any(e => e.Id == id);
        }
    }
}
