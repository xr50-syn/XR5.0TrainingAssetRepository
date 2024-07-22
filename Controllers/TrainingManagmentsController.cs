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
    [Route("/xr50/training-repo/training-management/[controller]")]
    [ApiController]
    public class TrainingController : ControllerBase
    {
        private readonly TrainingContext _context;
        private readonly HttpClient _httpClient;
        public TrainingController(TrainingContext context)
        {
            _context = context;
        }

        // GET: api/Training
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingActivity>>> GetTraining()
        {
            return await _context.Trainings.ToListAsync();
        }

        // GET: api/Training/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrainingActivity>> GetTraining(long id)
        {
            var Training = await _context.Trainings.FindAsync(id);

            if (Training == null)
            {
                return NotFound();
            }

            return Training;
        }

        // PUT: api/Training/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTraining(long id, TrainingActivity Training)
        {
            if (id != Training.TrainingId)
            {
                return BadRequest();
            }

            _context.Entry(Training).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TrainingExists(id))
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

        // POST: api/Training
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TrainingActivity>> PostTraining(TrainingActivity Training)
        {
            _context.Trainings.Add(Training);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTraining", new { id = Training.TrainingId }, Training);
        }

        // DELETE: api/Training/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTraining(long id)
        {
            var Training = await _context.Trainings.FindAsync(id);
            if (Training == null)
            {
                return NotFound();
            }

            _context.Trainings.Remove(Training);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TrainingExists(long id)
        {
            return _context.Trainings.Any(e => e.TrainingId == id);
        }
    }
}
