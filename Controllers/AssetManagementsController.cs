﻿using Microsoft.AspNetCore.Http;
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
    [Route("/xr50/training-repo/asset-management/[controller]")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly AssetContext _context;
       
        private readonly XR50AppContext _XR50AppContext;
        private readonly TrainingContext _xr50TrainingContext;
        private readonly ResourceContext _xr50ResourceContext;
        private readonly UserContext _userContext;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration; 
        public AssetController(AssetContext context, XR50AppContext XR50AppContext, UserContext UserManagementContext, TrainingContext xr50TrainingContext, ResourceContext xr50ResourceContext, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _XR50AppContext = XR50AppContext;
            _xr50TrainingContext = xr50TrainingContext;
            _xr50ResourceContext = xr50ResourceContext; 
            _userContext = UserManagementContext;
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
          
            var Training = await _xr50TrainingContext.Trainings.FindAsync(Asset.AppName, Asset.TrainingName);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50App = await _XR50AppContext.Apps.FindAsync(Asset.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }
            var admin = await _userContext.Users.FindAsync(XR50App.AdminName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Training.AppName}");
            }
           
            var Resource = await _xr50ResourceContext.Resource.FindAsync(Asset.AppName, Asset.TrainingName,Asset.ResourceName);
            string username = admin.UserName;
            string password = admin.Password; ;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            if (Resource != null)
            {
                Asset.OwncloudPath = $"{XR50App.OwncloudDirectory}/{Training.TrainingName}/{Resource.OwncloudFileName}/";
                
            } else
            {
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
            _context.Asset.Add(Asset);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostAsset", Asset);
        }

        // DELETE: api/Asset/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            var Asset = await _context.Asset.FindAsync(id);
            if (Asset == null)
            {
                return NotFound();
            }

            _context.Asset.Remove(Asset);
            await _context.SaveChangesAsync();

            var Training = await _xr50TrainingContext.Trainings.FindAsync(Asset.TrainingName);
            if (Training == null)
            {
                return NotFound();
            }
            var XR50App = await _XR50AppContext.Apps.FindAsync(Training.AppName);
            if (XR50App == null)
            {
                return NotFound();
            }
            var Resource = await _xr50ResourceContext.Resource.FindAsync(Asset.ResourceName);
            var admin = await _userContext.Users.FindAsync(XR50App.AdminName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Training.AppName}");
            }
            string username = admin.UserName;
            string password = admin.Password;
         
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	    string cmd= "curl";
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{XR50App.OwncloudDirectory}/\"";
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
            return _context.Asset.Any(e => e.AssetId == id);
        }
    }
}
