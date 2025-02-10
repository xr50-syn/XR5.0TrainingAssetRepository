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
     public class FileUploadFormData
     {
        public string? OwncloudFileName { get; set; }
        public string? TennantName { get; set; }
        public string? TrainingName { get; set; }
        public string? MaterialId { get; set; } 
        public string? Type { get; set; }
        public string? Description {get; set;}
        public IFormFile File { get; set; }
    }
    
    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
    [ApiController]
    public class owncloudFile_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public owncloudFile_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        // GET: api/OwncloudFiles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OwncloudFile>>> GetOwncloudFile()
        {
            return await _context.OwncloudFiles.ToListAsync();
        }

        // GET: api/OwncloudFiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OwncloudFile>> GetOwncloudFile(string id)
        {
            var OwncloudFile = await _context.OwncloudFiles.FindAsync(id);

            if (OwncloudFile == null)
            {
                return NotFound();
            }

            return OwncloudFile;
        }

        // PUT: api/OwncloudFiles/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> ShareFile(string id, Share share)
        {
             int shareType;
            if (share.Type == ShareType.Group)
            {
                
            } else
            {
                
            }
            var Owncloudfile = _context.OwncloudFiles.FindAsync(id);
            if (Owncloudfile == null) {
                Console.WriteLine($"Did not find File with id: {id}");
                return NotFound();
            }
            
            _context.Entry(Owncloudfile).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OwncloudFileExists(id))
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

        // POST: api/OwncloudFiles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<OwncloudFile>> PostOwncloudFile([FromForm] FileUploadFormData fileUpload)
        {
            OwncloudFile Owncloudfile= new OwncloudFile();
            Owncloudfile.Description=fileUpload.Description;
            Owncloudfile.OwncloudFileName=fileUpload.OwncloudFileName;
            Owncloudfile.TennantName=fileUpload.TennantName;
        
            _context.OwncloudFiles.Add(Owncloudfile);

            var XR50Tennant = await _context.Tennants.FindAsync(Owncloudfile.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Tennant {Owncloudfile.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Owncloudfile.TennantName}");
            }
            
            string username = admin.UserName;
            string password = admin.Password; ;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            
            string tempFileName=Path.GetTempFileName();
            using (var stream = System.IO.File.Create(tempFileName))
            {
                  await fileUpload.File.CopyToAsync(stream);
            }
	        string cmd="curl";
            string Arg= $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{tempFileName}\" \"{webdav_base}/{Owncloudfile.Path}/{Owncloudfile.OwncloudFileName}\"";
            // Create root dir for the Tennant
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
	        await _context.SaveChangesAsync();
            return CreatedAtAction("PostOwncloudFile", new { id = Owncloudfile.FileId }, Owncloudfile);
        }

        // DELETE: api/OwncloudFiles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOwncloudFile(string id)
        {
            var OwncloudFile = await _context.OwncloudFiles.FindAsync(id);
            if (OwncloudFile == null)
            {
                return NotFound();
            }

            _context.OwncloudFiles.Remove(OwncloudFile);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OwncloudFileExists(string id)
        {
            return _context.OwncloudFiles.Any(e => e.FileId == id);
        }
    }
}
