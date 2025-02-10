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
    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
    [ApiController]
    public class material_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        IConfiguration _configuration;  
        public material_managementController(XR50RepoContext context,HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 
        }

        // GET: api/ResourceManagements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetResource()
        {
            return await _context.Resources.ToListAsync();
        }

        // GET: api/ResourceManagements/5
        [HttpGet("{ResourceId}")]
        public async Task<ActionResult<Material>> GetResourceManagement(string ResourceId)
        {
            var Material = await _context.Resources.FindAsync(ResourceId);

            if (Material == null)
            {
                return NotFound();
            }

            return Material;
        }

       /* // PUT: api/ResourceManagements/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{ResourceId}")]
        public async Task<IActionResult> PutResourceManagement(string ResourceId, Material Material)
        {
            if (!ResourceId.Equals(Material.ResourceId))
            {
                return BadRequest();
            }

            _context.Entry(Material).State = EntityState.Modified;

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
*/
        // DELETE: api/ResourceManagements/5
        [HttpDelete("{ResourceId}")]
        public async Task<IActionResult> DeleteMaterialById(string ResourceId)
        {
            var Material = await _context.Resources.FindAsync(ResourceId);
            if (Material == null)
            {
                return NotFound();
            }

            _context.Resources.Remove(Material);
            await _context.SaveChangesAsync();

	        var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(Material.TrainingName) && t.TennantName.Equals(Material.TennantName));
            if (Training == null)
            {
                return NotFound();
            }
	        Training.ResourceList.Remove(ResourceId);
            var XR50Tennant = await _context.Apps.FindAsync(Training.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Training.TennantName}");
            }
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string ResourcePath= Material.ResourceName;
            if (Material.ParentType.Equals("RESOURCE")) {
            var ParentResource= await _context.Resources.FindAsync(Material.ParentId);
                while (ParentResource.ParentType.Equals("RESOURCE")) {
                    ResourcePath= ParentResource.ResourceName +"/" + ResourcePath;
                    ParentResource = await _context.Resources.FindAsync(ParentResource.ParentId);
                }
            }
	        string cmd="curl";
            string Arg= $"-X DELETE -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}/{ResourcePath}\"";
            // Create root dir for the App
            Console.WriteLine("Executing command:" + cmd + " " + Arg);
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
