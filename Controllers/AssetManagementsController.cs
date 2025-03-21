using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;
using System.Configuration;
using System.Threading.Tasks;

namespace XR5_0TrainingRepo.Controllers
{
    
    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
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
      /*  [HttpPut("{id}")]
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
*/
        // POST: api/Asset
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset(Asset asset)
        {

	        var XR50Tennant = await _context.Tennants.FindAsync(asset.TennantName);
           
            if (XR50Tennant == null)
            {
                return NotFound($"Tennant {asset.TennantName} Not Found");
            } 
            
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {asset.TennantName}");
            }
           
            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostAsset", asset);
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
            var XR50Tennant = await _context.Tennants.FindAsync(Asset.TennantName);     
            if (XR50Tennant == null)
            {
                return NotFound();
            }                                          
	        var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Asset.TennantName}");
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
