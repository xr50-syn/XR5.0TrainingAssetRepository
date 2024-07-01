using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/content-management/[controller]")]
    [ApiController]
    public class ContentController : ControllerBase
    {
        private readonly ContentContext _context;

        public ContentController(ContentContext context)
        {
            _context = context;
        }

        // GET: api/Content
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Content>>> GetContent()
        {
            return await _context.Content.ToListAsync();
        }

        // GET: api/Content/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Content>> GetContent(long id)
        {
            var Content = await _context.Content.FindAsync(id);

            if (Content == null)
            {
                return NotFound();
            }

            return Content;
        }

        // PUT: api/Content/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutContent(long id, Content Content)
        {
            if (id != Content.ContentId)
            {
                return BadRequest();
            }

            _context.Entry(Content).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContentExists(id))
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

        // POST: api/Content
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Content>> PostContent(Content Content)
        {
            _context.Content.Add(Content);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetContent", new { id = Content.ContentId }, Content);
        }

        // DELETE: api/Content/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContent(long id)
        {
            var Content = await _context.Content.FindAsync(id);
            if (Content == null)
            {
                return NotFound();
            }

            _context.Content.Remove(Content);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ContentExists(long id)
        {
            return _context.Content.Any(e => e.ContentId == id);
        }
    }
}
