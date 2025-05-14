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
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("/xr50/trainingAssetRepository/[controller]")]
    [ApiController]
    public class learningPathManagementController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        public learningPathManagementController(XR50TrainingAssetRepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 
        }

        // GET: api/LearningPath
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LearningPath>>> GetLearningPath()
        {
            return await _context.LearningPaths.ToListAsync();
        }

        // GET: api/LearningPath/5
        [HttpGet("{learningPathId}")]
        public async Task<ActionResult<LearningPath>> GetLearningPath(string tenantName,string learningPathId)
        {
            var LearningPath = await _context.LearningPaths.FindAsync(tenantName,learningPathId);

            if (LearningPath == null)
            {
                return NotFound();
            }
            return LearningPath;
        }
        //POST api/LearningPath/tennantName/ProgramName
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("{tenantName}")]
        public async Task<ActionResult<LearningPath>> PostLearningPath(string tenantName, LearningPath LearningPath)
        {
	       
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var XR50TrainingProgram = await _context.TrainingPrograms.FindAsync(tenantName, LearningPath.TrainingProgramName);
            if (XR50TrainingProgram == null)
            {
                return NotFound($"Couldnt Find Training Program {LearningPath.TrainingProgramName}");
            }
            
            
            LearningPath.LearningPathId = Guid.NewGuid().ToString();
            XR50TrainingProgram.LearningPathList.Add(LearningPath.LearningPathId);
 
            _context.LearningPaths.Add(LearningPath);
            await _context.SaveChangesAsync();

           
            return CreatedAtAction("PostLearningPath", LearningPath);
        }
        // PUT: api/LearningPath/5 
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{tenantName}/{learningPathId}")]
        public async Task<IActionResult> PutLearningPath(string learningPathId, LearningPath LearningPath)
        {
            if (!learningPathId.Equals(LearningPath.LearningPathId))
            {
                return BadRequest();
            }

            _context.Entry(LearningPath).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LearningPathExists(learningPathId))
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

        
        // DELETE: api/LearningPath/5
        [HttpDelete("{tenantName}/{learningPathName}")]
        public async Task<IActionResult> DeleteLearningPath(string tenantName,string learningPathId)
        {
            var LearningPath = await _context.LearningPaths.FindAsync(tenantName,learningPathId);
            if (LearningPath == null)
            {
                return NotFound();
            }
            var XR50Tenant = await _context.Tenants.FindAsync(LearningPath.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound();
            }
            _context.LearningPaths.Remove(LearningPath);
            
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        private bool LearningPathExists(string LearningPathId)
        {
            return _context.LearningPaths.Any(e => e.LearningPathId.Equals(LearningPathId));
        }
    }
}
