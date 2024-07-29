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
    [Route("/xr50/training-repo/ai-query/[controller]")]
    [ApiController]
    public class AI_Interface : ControllerBase
    {
        private readonly XRAIInterfaceContext _context;
        private readonly HttpClient _httpClient;
        public AI_Interface(XRAIInterfaceContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        // GET: api/AIQuery
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QueryStore>>> GetQueries()
        {
            return await _context.Queries.ToListAsync();
        }

        // GET: api/AIQuery/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QueryStore>> GetQueryStore(long id)
        {
            var queryStore = await _context.Queries.FindAsync(id);

            if (queryStore == null)
            {
                return NotFound();
            }

            return queryStore;
        }

        // PUT: api/AIQuery/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutQueryStore(long id, QueryStore queryStore)
        {
            if (id != queryStore.QueryId)
            {
                return BadRequest();
            }

            _context.Entry(queryStore).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QueryStoreExists(id))
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

        // POST: api/AIQuery
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<QueryStore>> PostQueryStore(QueryStore queryStore)
        {
            queryStore.QueryResponse = "I am an experimental Artificial Stupidity Singularity. Due to budget constraints I can only forward your request to HR and Legal.";
            _context.Queries.Add(queryStore);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetQueryStore", new { id = queryStore.QueryId }, queryStore);
        }

        // DELETE: api/AIQuery/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQueryStore(long id)
        {
            var queryStore = await _context.Queries.FindAsync(id);
            if (queryStore == null)
            {
                return NotFound();
            }

            _context.Queries.Remove(queryStore);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool QueryStoreExists(long id)
        {
            return _context.Queries.Any(e => e.QueryId == id);
        }
    }
}
