using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;
using System.Configuration;

namespace XR5_0TrainingRepo.Controllers
{
    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
    [ApiController]
    public class share_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public share_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
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
      /*  [HttpPut("{id}")]
        public async Task<IActionResult> PutOwncloudFile(string id, OwncloudFile OwncloudFile)
        {
            if (id != OwncloudFile.ShareId)
            {
                return BadRequest();
            }

            _context.Entry(OwncloudFile).State = EntityState.Modified;

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
*/
        // POST: api/OwncloudFiles
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<OwncloudFile>> PostOwncloudFile(OwncloudFile OwncloudFile)
        {
            _context.OwncloudFiles.Add(OwncloudFile);

            var XR50App = await _context.Apps.FindAsync(OwncloudFile.AppName);
            if (XR50App == null)
            {
                return NotFound($"App {OwncloudFile.AppName}");
            }
            var admin = await _context.Users.FindAsync(XR50App.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {OwncloudFile.AppName}");
            }
            string shareTarget;
            int shareType;
            if (OwncloudFile.Type == "Group")
            {
                shareTarget = XR50App.OwncloudGroup;
                shareType = 1;
            } else
            {
                shareTarget = OwncloudFile.Target;
                shareType = 0;
            }
            string assetId;
            if (OwncloudFile.AssetId== null)
            {
                assetId = "";
                return NotFound("No Asset ID provided to share");
            } else
            {
                assetId=OwncloudFile.AssetId;
            }
            var Asset = await _context.Assets.FindAsync(assetId);
            if (Asset==null)
            {
                    return NotFound($"Asset with {OwncloudFile.AssetId}");
            }
	        var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(Asset.TrainingName) && t.AppName.Equals(Asset.AppName));
            if (Training == null)
            {
                return NotFound($"Training for {OwncloudFile.TrainingName}");
            }
            OwncloudFile.OwncloudFileName=Asset.OwncloudFileName;
            OwncloudFile.Description=Asset.Description;
            OwncloudFile.TrainingName=Asset.TrainingName;
            
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
            return CreatedAtAction("GetOwncloudFile", new { id = OwncloudFile.ShareId }, OwncloudFile);
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
            return _context.OwncloudFiles.Any(e => e.ShareId == id);
        }
    }
}
