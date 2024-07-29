using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/training-repo/training-management/[controller]")]
    [ApiController]
    public class TrainingController : ControllerBase
    {
        private readonly TrainingContext _context;
        private readonly XR50AppContext _xr50AppContext;
        private readonly HttpClient _httpClient;
        public TrainingController(TrainingContext context, XR50AppContext xr50AppContext, HttpClient httpClient)
        {
            _context = context;
            _xr50AppContext = xr50AppContext;
            _httpClient = httpClient;
        }

        // GET: api/Training
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingModule>>> GetTraining()
        {
            return await _context.Trainings.ToListAsync();
        }

        // GET: api/Training/5
        [HttpGet("{TrainingName}")]
        public async Task<ActionResult<TrainingModule>> GetTraining(long TrainingName)
        {
            var Training = await _context.Trainings.FindAsync(TrainingName);

            if (Training == null)
            {
                return NotFound();
            }

            return Training;
        }

        // PUT: api/Training/5 
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{TrainingName}")]
        public async Task<IActionResult> PutTraining(string TrainingName, TrainingModule Training)
        {
            if (!TrainingName.Equals(Training.TrainingName))
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
                if (!TrainingExists(TrainingName))
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
        public async Task<ActionResult<TrainingModule>> PostTraining(TrainingModule Training)
        {
            var xR50App = await _xr50AppContext.Apps.FindAsync(Training.AppName);
            if (xR50App == null)
            {
                return NotFound();
            }

            _context.Trainings.Add(Training);
            await _context.SaveChangesAsync();

            string username = "emmie";
            string password = "!@m!nL0v3W!th@my";
            // Createe root dir for the Training
            string cmd = $"/C curl -X MKCOL -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\"  \"http://192.168.169.6:8080/remote.php/webdav/{xR50App.OwncloudDirectory}/{Training.TrainingName}\"";
            Console.WriteLine(cmd);
            System.Diagnostics.Process.Start("CMD.exe", cmd);

            return CreatedAtAction("GetTraining", new { Training.TrainingName });
        }

        // DELETE: api/Training/5
        [HttpDelete("{TrainingName}")]
        public async Task<IActionResult> DeleteTraining(string TrainingName)
        {
            var Training = await _context.Trainings.FindAsync(TrainingName);
            if (Training == null)
            {
                return NotFound();
            }

            _context.Trainings.Remove(Training);
            await _context.SaveChangesAsync();

            var xR50App = await _xr50AppContext.Apps.FindAsync(Training.AppName);
            if (xR50App == null)
            {
                return NotFound();
            }

            string username = "emmie";
            string password = "!@m!nL0v3W!th@my";
            // Createe root dir for the Training
            string cmd = $"/C curl -X DELETE -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\"  \"http://192.168.169.6:8080/remote.php/webdav/{xR50App.OwncloudDirectory}/{Training.TrainingName}\"";
            Console.WriteLine(cmd);
            System.Diagnostics.Process.Start("CMD.exe", cmd);
            return NoContent();
        }

        private bool TrainingExists(string TrainingName)
        {
            return _context.Trainings.Any(e => e.TrainingName.Equals(TrainingName));
        }
    }
}
