 using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
    [Route("/xr50/training-repo/[controller]")]
    [ApiController]
    public class ResourceManagementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        IConfiguration _configuration;  
        public ResourceManagementController(XR50RepoContext context,HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 
        }

        // GET: api/ResourceManagements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResourceManagement>>> GetResource()
        {
            return await _context.Resources.ToListAsync();
        }

        // GET: api/ResourceManagements/5
        [HttpGet("{AppName}/{TrainingName}/{ResourceName}")]
        public async Task<ActionResult<ResourceManagement>> GetResourceManagement(string AppName, string TrainingName, string ResourceName)
        {
            var resourceManagement = await _context.Resources.FindAsync(ResourceName);

            if (resourceManagement == null)
            {
                return NotFound();
            }

            return resourceManagement;
        }

        // PUT: api/ResourceManagements/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{ResourceId}")]
        public async Task<IActionResult> PutResourceManagement(string ResourceId, ResourceManagement resourceManagement)
        {
            if (!ResourceId.Equals(resourceManagement.ResourceId))
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
                if (!ResourceManagementExists(ResourceId))
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
        [HttpPost("{AppName}/{TrainingName}")]
        public async Task<ActionResult<ResourceManagement>> PostResourceManagement(string AppName, string TrainingName, ResourceManagement resourceManagement)
        {

            var XR50App = await _context.Apps.FindAsync(resourceManagement.AppName);
            if (XR50App == null)
            {
                return NotFound($"App {resourceManagement.AppName}");
            }
            var admin = await _context.Users.FindAsync(XR50App.AdminName);
            if (admin == null)
            {
                return NotFound($"Admin user for {resourceManagement.AppName}");
            }
            var Training = await _context.Trainings.FindAsync(resourceManagement.AppName, resourceManagement.TrainingName);
            if (Training == null)
            {
                return NotFound($"Training for {resourceManagement.TrainingName}");
            }
            resourceManagement.ResourceId = Guid.NewGuid().ToString();
            Training.ResourceList.Add(resourceManagement.ResourceId);
            _context.Resources.Add(resourceManagement);
            await _context.SaveChangesAsync();
           
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd="curl";
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{resourceManagement.OwncloudFileName}\"";
            // Create root dir for the App
            Console.WriteLine("Ececuting command:" + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = Arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("Output: " + output);
                Console.WriteLine("Error: " + error);
            } 
            Training.ResourceList.Add(resourceManagement.ResourceId);
            
            _context.SaveChanges();
            return CreatedAtAction("PostResourceManagement", resourceManagement);
        }

        // DELETE: api/ResourceManagements/5
        [HttpDelete("{AppName}/{TrainingName}/{ResourceName}")]
        public async Task<IActionResult> DeleteResourceManagement(string AppName, string TrainingName, string ResourceName)
        {
            var resourceManagement = await _context.Resources.FindAsync(AppName, TrainingName, ResourceName);
            if (resourceManagement == null)
            {
                return NotFound();
            }

            _context.Resources.Remove(resourceManagement);
            await _context.SaveChangesAsync();

            var Training = await _context.Trainings.FindAsync(resourceManagement.AppName,resourceManagement.TrainingName);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50App = await _context.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50App.AdminName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Training.AppName}");
            }
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	    string cmd="curl";
            string Arg= $"-X DELETE -u {username}:{password} \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{resourceManagement.OwncloudFileName}\"";
            // Create root dir for the App
            Console.WriteLine("Ececuting command:" + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = Arg,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (var process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                Console.WriteLine("Output: " + output);
                Console.WriteLine("Error: " + error);
            }
            return NoContent();
        }

        private bool ResourceManagementExists(string ResourceName)
        {
            return _context.Resources.Any(e => e.ResourceName.Equals(ResourceName));
        }
    }
}
