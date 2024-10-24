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
    public class OwncloudSharesController : ControllerBase
    {
        private readonly XR50AppContext _context;

        public OwncloudSharesController(XR50AppContext context)
        {
            _context = context;
        }

        // GET: api/OwncloudShares
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OwncloudShare>>> GetOwncloudShare()
        {
            return await _context.OwncloudShare.ToListAsync();
        }

        // GET: api/OwncloudShares/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OwncloudShare>> GetOwncloudShare(string id)
        {
            var owncloudShare = await _context.OwncloudShare.FindAsync(id);

            if (owncloudShare == null)
            {
                return NotFound();
            }

            return owncloudShare;
        }

        // PUT: api/OwncloudShares/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOwncloudShare(string id, OwncloudShare owncloudShare)
        {
            if (id != owncloudShare.ShareId)
            {
                return BadRequest();
            }

            _context.Entry(owncloudShare).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OwncloudShareExists(id))
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

        // POST: api/OwncloudShares
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<OwncloudShare>> PostOwncloudShare(OwncloudShare owncloudShare)
        {
            _context.OwncloudShare.Add(owncloudShare);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (OwncloudShareExists(owncloudShare.ShareId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetOwncloudShare", new { id = owncloudShare.ShareId }, owncloudShare);
        }

        // DELETE: api/OwncloudShares/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOwncloudShare(string id)
        {
            var owncloudShare = await _context.OwncloudShare.FindAsync(id);
            if (owncloudShare == null)
            {
                return NotFound();
            }

            _context.OwncloudShare.Remove(owncloudShare);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OwncloudShareExists(string id)
        {
            return _context.OwncloudShare.Any(e => e.ShareId == id);
        }
    }
}
