using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/Training_Asset_Repository/[controller]")]
    [ApiController]
    public class training_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public training_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
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
        [HttpGet("{TenantName}/{TrainingName}")]
        public async Task<ActionResult<TrainingModule>> GetTraining(string TenantName,string TrainingName)
        {
            var Training = await _context.Trainings.FindAsync(TenantName,TrainingName);

            if (Training == null)
            {
                return NotFound();
            }
            return Training;
        }

        // PUT: api/Training/5 
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
       /* [HttpPut("{TenantName}/{TrainingName}")]
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
*/
        
        // DELETE: api/Training/5
        [HttpDelete("{TenantName}/{TrainingName}")]
        public async Task<IActionResult> DeleteTraining(string TenantName,string TrainingName)
        {
            var Training = await _context.Trainings.FindAsync(TenantName,TrainingName);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50Tenant = await _context.Tenants.FindAsync(Training.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound();
            }
            _context.Trainings.Remove(Training);
            XR50Tenant.TrainingList.Remove(Training.TrainingName);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool TrainingExists(string TrainingName)
        {
            return _context.Trainings.Any(e => e.TrainingName.Equals(TrainingName));
        }
    }
}
