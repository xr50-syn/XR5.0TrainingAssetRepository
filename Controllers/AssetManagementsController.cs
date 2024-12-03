using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/magical_library/[controller]")]
    [ApiController]
    public class asset_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration; 
        public asset_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 

        }
        
        // GET: api/Asset
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAsset()
        {
            return await _context.Assets.ToListAsync();
        }

        // GET: api/Asset/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(string id)
        {
            var Asset = await _context.Assets.FindAsync(id);

            if (Asset == null)
            {
                return NotFound();
            }

            return Asset;
        }

        // PUT: api/Asset/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsset(string id, Asset Asset)
        {
            if (id != Asset.AssetId)
            {
                return BadRequest();
            }

            _context.Entry(Asset).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(id))
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

        // POST: api/Asset
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset(Asset Asset)
        {

	    var XR50App = await _context.Apps.FindAsync(Asset.AppName);
            if (XR50App == null)
            {
                return NotFound();
            } 
            var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(Asset.TrainingName) && t.AppName.Equals(Asset.AppName)); 
            if (Training == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50App.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Training.AppName}");
            }
           
            string username = admin.UserName;
            string password = admin.Password; ;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            if (Asset.ResourceName != null)
            {
		        var Resource = _context.Resources.FirstOrDefault( r => r.ResourceName.Equals(Asset.ResourceName) && r.TrainingName.Equals(Asset.TrainingName) && r.AppName.Equals(Asset.AppName));
		        Resource.AssetList.Add(Asset.AssetId);
                Asset.OwncloudPath = $"{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Resource.OwncloudFileName}/";
                
            } else
            {
		        Training.AssetList.Add(Asset.AssetId);
                Asset.OwncloudPath = $"{XR50App.OwncloudDirectory}/{Training.TrainingName}/";
            }
	    string cmd="curl";
            string Arg= $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{Asset.Path}\" \"{webdav_base}/{Asset.OwncloudPath}/{Asset.OwncloudFileName}\"";
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
            _context.Assets.Add(Asset);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostAsset", Asset);
        }

        // DELETE: api/Asset/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            var Asset = await _context.Assets.FindAsync(id);
            if (Asset == null)
            {
                return NotFound();
            }
            var XR50App = await _context.Apps.FindAsync(Asset.AppName);                                                             if (XR50App == null)                                                                                                    {                                                                                                                           return NotFound();                                                                                                  }
	    var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(Asset.TrainingName) && t.AppName.Equals(Asset.AppName));                                                                                                             if (Training == null)                                                                                                   {                                                                                                                           return NotFound();                                                                                                  }                                                                                                                       var admin = await _context.Users.FindAsync(XR50App.OwnerName);                                                          if (admin == null)                                                                                                      {                                                                                                                           return NotFound($"Admin user for {Training.AppName}");                                                              }
	    if (Asset.ResourceName!=null) {
	    	var Resource = _context.Resources.FirstOrDefault( r => r.ResourceName.Equals(Asset.ResourceName) && r.TrainingName.Equals(Asset.TrainingName) &&r.AppName.Equals(Asset.AppName));                                                              Resource.AssetList.Remove(Asset.AssetId);                                                             
	    } else {
		Training.AssetList.Remove(Asset.AssetId);
	    }




            string username = admin.UserName;
            string password = admin.Password;
             _context.Assets.Remove(Asset);                                                                                          await _context.SaveChangesAsync(); 
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	    string cmd= "curl";
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{Asset.OwncloudPath}/{Asset.OwncloudFileName}\"";
            Console.WriteLine("Executing command: " + cmd + " " + Arg);
            var startInfo = new ProcessStartInfo
            {                                                                                                                           FileName = cmd,
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

        private bool AssetExists(string id)
        {
            return _context.Assets.Any(e => e.AssetId == id);
        }
    }
}
