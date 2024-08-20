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
    [Route("/xr50/training-repo/asset-management/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly AssetContext _context;
        private readonly XR50AppContext _XR50AppContext;
        private readonly TrainingContext _xr50TrainingContext;
        private readonly ResourceContext _xr50ResourceContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration; 
        public AssetController(AssetContext context, XR50AppContext XR50AppContext, TrainingContext xr50TrainingContext, ResourceContext xr50ResourceContext, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _XR50AppContext = XR50AppContext;
            _xr50TrainingContext = xr50TrainingContext;
            _xr50ResourceContext = xr50ResourceContext; 
            _httpClient = httpClient;
            _configuration = configuration; 

        }
        
        // GET: api/Asset
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAsset()
        {
            return await _context.Asset.ToListAsync();
        }

        // GET: api/Asset/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(long id)
        {
            var Asset = await _context.Asset.FindAsync(id);

            if (Asset == null)
            {
                return NotFound();
            }

            return Asset;
        }

        // PUT: api/Asset/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsset(long id, Asset Asset)
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
            _context.Asset.Add(Asset);
            await _context.SaveChangesAsync();

            var Training = await _xr50TrainingContext.Trainings.FindAsync(Asset.TrainingId);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50App = await _XR50AppContext.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }
            var Resource = await _xr50ResourceContext.Resource.FindAsync(Asset.ResourceId);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd;
            if (Resource != null)
            {
                cmd = $"/C curl -X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{Asset.Path}\" \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Resource.OwncloudFileName}/{Asset.OwncloudFileName}\"";
            } else
            {
                cmd = $"/C curl -X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{Asset.Path}\" \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Asset.OwncloudFileName}\"";
            }
            Console.WriteLine(cmd);
            System.Diagnostics.Process.Start("CMD.exe", cmd);
            return CreatedAtAction("GetAsset", new { id = Asset.AssetId }, Asset);
        }

        // DELETE: api/Asset/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(long id)
        {
            var Asset = await _context.Asset.FindAsync(id);
            if (Asset == null)
            {
                return NotFound();
            }

            _context.Asset.Remove(Asset);
            await _context.SaveChangesAsync();

            var Training = await _xr50TrainingContext.Trainings.FindAsync(Asset.TrainingId);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50App = await _XR50AppContext.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }
            var Resource = await _xr50ResourceContext.Resource.FindAsync(Asset.ResourceId);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd;
            if (Resource != null)
            {
                cmd = $"/C curl -X DELETE -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{Asset.Path}\" \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Resource.OwncloudFileName}/{Asset.OwncloudFileName}\"";
            }
            else
            {
                cmd = $"/C curl -X DELETE -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{Asset.Path}\" \"{webdav_base}/{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Asset.OwncloudFileName}\"";
            }
            Console.WriteLine(cmd);
            System.Diagnostics.Process.Start("CMD.exe", cmd);
            return NoContent();
        }

        private bool AssetExists(long id)
        {
            return _context.Asset.Any(e => e.AssetId == id);
        }
    }
}
