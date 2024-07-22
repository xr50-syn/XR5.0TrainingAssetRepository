using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/Asset-management/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly AssetContext _context;
        private readonly HttpClient _httpClient;
        public AssetController(AssetContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // GET: api/Asset
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAsset()
        {
            return await _context.Asset.ToListAsync();
        }

        // GET: api/Asset/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(long id)
        {
            var Asset = await _context.Asset.FindAsync(id);

            if (Asset == null)
            {
                return NotFound();
            }

            return Asset;
        }

        // PUT: api/Asset/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsset(long id, Asset Asset)
        {
            if (id != Asset.AssetId)
            {
                return BadRequest();
            }

            _context.Entry(Asset).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(id))
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

        // POST: api/Asset
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset(Asset Asset)
        {
            _context.Asset.Add(Asset);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAsset", new { id = Asset.AssetId }, Asset);
        }

        // DELETE: api/Asset/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(long id)
        {
            var Asset = await _context.Asset.FindAsync(id);
            if (Asset == null)
            {
                return NotFound();
            }

            _context.Asset.Remove(Asset);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssetExists(long id)
        {
            return _context.Asset.Any(e => e.AssetId == id);
        }
    }
}
