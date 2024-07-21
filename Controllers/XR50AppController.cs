using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class XR50AppController : ControllerBase
    {
        private readonly XR50AppContext _context;

        public XR50AppController(XR50AppContext context)
        {
            _context = context;
        }

        // GET: api/XR50App
        [HttpGet]
        public async Task<ActionResult<IEnumerable<XR50App>>> GetApps()
        {
            return await _context.Apps.ToListAsync();
        }

        // GET: api/XR50App/5
        [HttpGet("{id}")]
        public async Task<ActionResult<XR50App>> GetXR50App(long id)
        {
            var xR50App = await _context.Apps.FindAsync(id);

            if (xR50App == null)
            {
                return NotFound();
            }

            return xR50App;
        }

        // PUT: api/XR50App/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutXR50App(long id, XR50App xR50App)
        {
            if (id != xR50App.AppId)
            {
                return BadRequest();
            }

            _context.Entry(xR50App).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!XR50AppExists(id))
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

        // POST: api/XR50App
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<XR50App>> PostXR50App(XR50App xR50App)
        {
            _context.Apps.Add(xR50App);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetXR50App", new { id = xR50App.AppId }, xR50App);
        }

        // DELETE: api/XR50App/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteXR50App(long id)
        {
            var xR50App = await _context.Apps.FindAsync(id);
            if (xR50App == null)
            {
                return NotFound();
            }

            _context.Apps.Remove(xR50App);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool XR50AppExists(long id)
        {
            return _context.Apps.Any(e => e.AppId == id);
        }
    }
}
