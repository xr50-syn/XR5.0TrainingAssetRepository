using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/resource-management/[controller]")]
    [ApiController]
    public class ResourceManagementController : ControllerBase
    {
        private readonly ResourceContext _context;
        private readonly HttpClient _httpClient;
        public ResourceManagementController(ResourceContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // GET: api/ResourceManagements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceManagement>>> GetResource()
        {
            return await _context.Resource.ToListAsync();
        }

        // GET: api/ResourceManagements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ResourceManagement>> GetResourceManagement(long id)
        {
            var resourceManagement = await _context.Resource.FindAsync(id);

            if (resourceManagement == null)
            {
                return NotFound();
            }

            return resourceManagement;
        }

        // PUT: api/ResourceManagements/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResourceManagement(long id, ResourceManagement resourceManagement)
        {
            if (id != resourceManagement.ResourceId)
            {
                return BadRequest();
            }

            _context.Entry(resourceManagement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ResourceManagementExists(id))
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

        // POST: api/ResourceManagements
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<ResourceManagement>> PostResourceManagement(ResourceManagement resourceManagement)
        {
            _context.Resource.Add(resourceManagement);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetResourceManagement", new { id = resourceManagement.ResourceId }, resourceManagement);
        }

        // DELETE: api/ResourceManagements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResourceManagement(long id)
        {
            var resourceManagement = await _context.Resource.FindAsync(id);
            if (resourceManagement == null)
            {
                return NotFound();
            }

            _context.Resource.Remove(resourceManagement);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ResourceManagementExists(long id)
        {
            return _context.Resource.Any(e => e.ResourceId == id);
        }
    }
}
