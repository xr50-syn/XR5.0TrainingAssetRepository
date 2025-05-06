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
using Microsoft.AspNetCore.Authorization;

namespace XR50TrainingAssetRepo.Controllers
{

    [Route("/xr50/TrainingProgram_Asset_Repository/[controller]")]
    [ApiController]
    
    public class tenant_managementController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
     
        public tenant_managementController(XR50TrainingAssetRepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration= configuration;
        }

        // GET: api/XR50Tenant
        [HttpGet]
        public async Task<ActionResult<IEnumerable<XR50Tenant>>> GetTenants()
        {
            return await _context.Tenants.ToListAsync();
        }
        
        // GET: api/XR50Tenant/5
        [HttpGet("{TenantName}")]
        public async Task<ActionResult<XR50Tenant>> GetXR50Tenant(string TenantName)
        {
            var XR50Tenant = await _context.Tenants.FindAsync(TenantName);

            if (XR50Tenant == null)
            {
                return NotFound();
            }

            return XR50Tenant;
        }
        // GET: api/XR50Tenant/5
        [HttpGet("{TenantName}/TrainingPrograms")]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetTenantTrainingPrograms(string TenantName)
        {
            return _context.TrainingPrograms.Where(t=>t.TenantName.Equals(TenantName)).ToList();
        }
        [HttpGet("{TenantName}/Materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetTenantMaterials(string TenantName)
        {
            return _context.Materials.Where(t=>t.TenantName.Equals(TenantName)).ToList();
        }

        // GET: api/XR50Tenant/5

        /*
        // PUT: api/XR50Tenant/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutXR50Tenant(long id, XR50Tenant XR50Tenant)
        {
            if (id != XR50Tenant.TenantId)
            {
                return BadRequest();
            }

            _context.Entry(XR50Tenant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!XR50TenantExists(id))
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

        // POST: api/XR50Tenant
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<XR50Tenant>> PostXR50Tenant(XR50Tenant XR50Tenant)
        {

            _context.Tenants.Add(XR50Tenant);

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("groupid", XR50Tenant.OwncloudGroup));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Post, uri_path)
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
           // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            User adminUser = XR50Tenant.Owner;
            XR50Tenant.OwnerName = adminUser.UserName;
	        XR50Tenant.AdminList.Add(adminUser.UserName);
            _context.Users.Add(adminUser);
            _context.SaveChanges();
            //Console.WriteLine($"Response content: {resultContent}");
            //Create the admin User
            var valuesAdmin = new List<KeyValuePair<string, string>>();
            valuesAdmin.Add(new KeyValuePair<string, string>("userid", adminUser.UserName));
            valuesAdmin.Add(new KeyValuePair<string, string>("password", adminUser.Password));
            valuesAdmin.Add(new KeyValuePair<string, string>("email", adminUser.UserEmail));
            valuesAdmin.Add(new KeyValuePair<string, string>("display", adminUser.FullName));
            valuesAdmin.Add(new KeyValuePair<string, string>("groups[]", XR50Tenant.OwncloudGroup));
            //Target The User Interface
            uri_path = _configuration.GetValue<string>("OwncloudSettings:UserManagementPath");
            FormUrlEncodedContent messageContentAdmin = new FormUrlEncodedContent(valuesAdmin);
           
            var requestAdmin = new HttpRequestMessage(HttpMethod.Post, uri_path)
            {
                Content = messageContentAdmin
            };
            requestAdmin.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
           
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var resultAdmin = _httpClient.SendAsync(requestAdmin).Result;
            string resultAdminContent = resultAdmin.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Response content: {resultAdminContent}");

            // Create root dir for the Tenant, owned by Admin
	        string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{dirl}/\"";
            // Create root dir for the Tenant
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
            _context.SaveChanges();
            return CreatedAtAction("PostXR50Tenant", XR50Tenant);
        }
        // POST: api/XR50Tenant/
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/training-management/{TenantName}")]
        public async Task<ActionResult<TrainingProgram>> PostTrainingProgram(string TenantName,TrainingProgram TrainingProgram)
        {
	        if (!TenantName.Equals(TrainingProgram.TenantName)) {
		        return NotFound($"Couldnt Find Tenant {TrainingProgram.TenantName} is not our parent");
	        }
            var XR50Tenant = await _context.Tenants.FindAsync(TrainingProgram.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {TrainingProgram.TenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin ==null) 
            {
                return NotFound($"Couldnt Find Admin user for {TrainingProgram.TenantName}");
            }

            XR50Tenant.TrainingProgramList.Add(TrainingProgram.TrainingProgramName); 
            _context.TrainingPrograms.Add(TrainingProgram);
            await _context.SaveChangesAsync();

           
            return CreatedAtAction("PostTrainingProgram", TrainingProgram);
        }
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/material-management/{TenantName}/{ParentMaterialId}")]
        public async Task<ActionResult<Material>> PostChildMaterial(string TenantName, string ParentMaterialId, Material Material)
        {

            var XR50Tenant = await _context.Tenants.FindAsync(TenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {TenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Material.TenantName}");
            }
           
            var ParentMaterial = await _context.Materials.FindAsync(ParentMaterialId);
            if (ParentMaterial == null) {
                return NotFound($"Couldnt Find Material with Id: {ParentMaterialId}");
            }
            Material.MaterialId = Guid.NewGuid().ToString();
            
            Material.ParentId =ParentMaterial.MaterialId;
            ParentMaterial.MaterialList.Add(Material.MaterialId);
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostChildMaterial", TenantName, Material);
        }

        // DELETE: api/XR50Tenant/5
        [HttpDelete("{TenantName}")]
        public async Task<IActionResult> DeleteXR50Tenant(string TenantName)
        {
            var XR50Tenant = await _context.Tenants.FindAsync(TenantName);
            if (XR50Tenant == null)
            {
                Console.WriteLine($"Did not find XR app with id: {TenantName}");
                return NotFound();
            }
            var adminUser = await _context.Users.FindAsync(XR50Tenant.OwnerName); 
	        if (adminUser == null) {
		        Console.WriteLine($"Did not find Owner: {XR50Tenant.OwnerName}");
                return NotFound();
            }

            foreach (string trainingName in XR50Tenant.TrainingProgramList) {
              var training= await _context.TrainingPrograms.FindAsync(TenantName,trainingName);
              foreach (string resourceId in training.MaterialList) {
                var resource= await _context.Materials.FindAsync(resourceId);
                _context.Materials.Remove(resource);
              }     
              _context.TrainingPrograms.Remove(training);
            }
            _context.Tenants.Remove(XR50Tenant);
	        _context.Users.Remove(adminUser);
            await _context.SaveChangesAsync();

            var values = new List<KeyValuePair<string, string>>();
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("OwncloudSettings:Admin");
            string password = _configuration.GetValue<string>("OwncloudSettings:Password");
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_group = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
	        string uri_user = _configuration.GetValue<string>("OwncloudSettings:UserManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_group}/{XR50Tenant.OwncloudGroup}")
            {
                Content = messageContent
            };
            Console.WriteLine(XR50Tenant.OwncloudGroup);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            //_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            // Delete root dir for the Tenant
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.OwncloudDirectory);
            string Arg=  $"-X DELETE -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{dirl}/\"";
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
            request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_user}/{XR50Tenant.OwnerName}")
            {
                Content = messageContent
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            // _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
	        result = _httpClient.SendAsync(request).Result;
            resultContent = result.Content.ReadAsStringAsync().Result;    
            //Console.WriteLine($"Response content: {resultContent}");
            return NoContent();
        }
        [HttpDelete("/xr50/TrainingProgram_Asset_Repository/[controller]/training-management/{TenantName}/{TrainingProgramName}")]
        public async Task<IActionResult> DeleteTrainingProgram(string TenantName,string TrainingProgramName)
        {
            var TrainingProgram = await _context.TrainingPrograms.FindAsync(TenantName,TrainingProgramName);
            if (TrainingProgram == null)           
            {
                return NotFound($"Did not find training {TrainingProgramName}");
            }

            var XR50Tenant = await _context.Tenants.FindAsync(TenantName);
            if (XR50Tenant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {TrainingProgram.TenantName}");
            }
            foreach (string resourceId in TrainingProgram.MaterialList) {
                var resource= await _context.Materials.FindAsync(resourceId);
                _context.Materials.Remove(resource);
              }     
            _context.TrainingPrograms.Remove(TrainingProgram);
            XR50Tenant.TrainingProgramList.Remove(TrainingProgram.TrainingProgramName);
            await _context.SaveChangesAsync();

            //Owncloud stuff
            string username = admin.UserName;
            string password = admin.Password;
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            
            // Remove root dir for the TrainingProgram
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.OwncloudDirectory);
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{dirl}/{TrainingProgram.TrainingProgramName}\"";
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


        // DELETE: api/XR50Tenant/
        [HttpDelete("/xr50/TrainingProgram_Asset_Repository/[controller]/material-management/{TenantName}/{TrainingProgramName}/{MateriaId}")]
        public async Task<IActionResult> DeleteMaterial(string TenantName, string TrainingProgramName, string MaterialId)
        {
            var Material = await _context.Materials.FindAsync(MaterialId);
            if (Material == null)
            {
                return NotFound();
            }

            _context.Materials.Remove(Material);
            await _context.SaveChangesAsync();

	        var TrainingProgram = _context.TrainingPrograms.FirstOrDefault(t=> t.TrainingProgramName.Equals(TrainingProgramName) && t.TenantName.Equals(TenantName));
            if (TrainingProgram == null)
            {
                return NotFound();
            }
	        TrainingProgram.MaterialList.Remove(Material.MaterialId);
            var XR50Tenant = await _context.Tenants.FindAsync(TrainingProgram.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {TrainingProgram.TenantName}");
            }
            return NoContent();
        }
        private bool XR50TenantExists(string TenantName)
        {
            return _context.Tenants.Any(e => e.TenantName.Equals(TenantName));
        }
    }
}
