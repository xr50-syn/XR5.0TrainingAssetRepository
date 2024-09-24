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
        private readonly XR50AppContext _XR50AppContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public TrainingController(TrainingContext context, XR50AppContext XR50AppContext, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _XR50AppContext = XR50AppContext;
            _httpClient = httpClient;
            _configuration = configuration; 
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
            var XR50App = await _XR50AppContext.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }

            _context.Trainings.Add(Training);
            await _context.SaveChangesAsync();

            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd = $"/C curl -X MKCOL -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\"  \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}\"";
            Console.WriteLine(cmd);
            System.Diagnostics.Process.Start("CMD.exe", cmd);

            return CreatedAtAction("PostTraining", new { Training.TrainingName });
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

            var XR50App = await _XR50AppContext.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }

            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd = $"/C curl -X DELETE -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\"  \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}\"";
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
