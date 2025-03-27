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
        public string? TennantName { get; set; }
        public string? ParentId { get; set; }
        public string TrainingName {get; set;} 
        public string? OwncloudPath {get;set;}
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
        public async Task<ActionResult<IEnumerable<Asset>>> GetAsset()
        {
            return await _context.Assets.ToListAsync();
        }

        // GET: api/Assets/5
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
        [HttpGet("/xr50/library_of_reality_altering_knowledge/[controller]/share")]
        public async Task<ActionResult<IEnumerable<Share>>> GetShare()
        {
            return await _context.Shares.ToListAsync();
        }
        // PUT: api/Assets/5
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
            var Owncloudfile = _context.Assets.FindAsync(id);
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

        // POST: api/Assets
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Asset>> PostAsset([FromForm] FileUploadFormData fileUpload)
        {
            Asset Owncloudfile= new Asset();
            Owncloudfile.Description=fileUpload.Description;
            Owncloudfile.TennantName=fileUpload.TennantName;
            Owncloudfile.OwncloudPath= fileUpload.OwncloudPath;
            
            if (fileUpload.Type != null) {
                Owncloudfile.OwncloudFileName += $".{fileUpload.Type}";
            }
            _context.Assets.Add(Owncloudfile);

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
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{tempFileName}\" \"{webdav_base}/{dirl}/{Owncloudfile.OwncloudFileName}\"";
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
            return CreatedAtAction("PostAsset", new { id = Owncloudfile.OwncloudFileName }, Owncloudfile);
        }
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/directory")]
        public async Task<IActionResult> PostDirectory(OwncloudDirectory directory){

            var XR50Tennant = await _context.Tennants.FindAsync(directory.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Tennant {directory.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {directory.TennantName}");
            }
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	        string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{dirl}/{directory.OwncloudPath}\"";
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
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/share")]
        public async Task<ActionResult<Share>> PostShare(Share owncloudShare)
        {
            _context.Shares.Add(owncloudShare);

            var XR50Tennant = await _context.Tennants.FindAsync(owncloudShare.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"App {owncloudShare.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {owncloudShare.TennantName}");
            }
            string shareTarget;
            int shareType;
            if (owncloudShare.Type == ShareType.Group)
            {
                shareTarget = XR50Tennant.OwncloudGroup;
                shareType = 1;
            } else
            {
                shareTarget = owncloudShare.Target;
                shareType = 0;
            }
            string OwncloudFileName;
            if (owncloudShare.FileId== null)
            {
                OwncloudFileName = "";
                return NotFound("No File ID provided to share");
            } else
            {
                OwncloudFileName=owncloudShare.FileId;
            }
            var Asset = await _context.Assets.FindAsync(OwncloudFileName);
            if (Asset==null)
            {
                    return NotFound($"File with {owncloudShare.FileId}");
            }
	    
            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("shareType", shareType.ToString()));
            values.Add(new KeyValuePair<string, string>("shareWith", shareTarget));
            values.Add(new KeyValuePair<string, string>("permissions", 1.ToString()));
            values.Add(new KeyValuePair<string, string>("path", $"{Asset.OwncloudPath}/{Asset.OwncloudFileName}"));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = admin.UserName;
            string password = admin.Password;

            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_share = _configuration.GetValue<string>("OwncloudSettings:ShareManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Post, uri_share)
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            //Console.WriteLine(resultContent);
	        await _context.SaveChangesAsync();
            return CreatedAtAction("PostShare", new { id = owncloudShare.ShareId }, owncloudShare);
        }

        // DELETE: api/Shares/5
        [HttpDelete("/xr50/library_of_reality_altering_knowledge/[controller]/share/{id}")]
        public async Task<IActionResult> DeleteShare(string id)
        {
            var owncloudShare = await _context.Shares.FindAsync(id);
            if (owncloudShare == null)
            {
                return NotFound();
            }

            _context.Shares.Remove(owncloudShare);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Assets/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(string id)
        {
            var Asset = await _context.Assets.FindAsync(id);
            if (Asset == null)
            {
                return NotFound();
            }

            _context.Assets.Remove(Asset);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool ShareExists(string id)
        {
            return _context.Shares.Any(e => e.ShareId == id);
        }
        private bool AssetExists(string id)
        {
            return _context.Assets.Any(e => e.OwncloudFileName == id);
        }
    }
}
