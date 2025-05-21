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

    [Route("/xr50/trainingAssetRepository/[controller]")]
    [ApiController]
    
    public class tenantsController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
     
        public tenantsController(XR50TrainingAssetRepoContext context, HttpClient httpClient, IConfiguration configuration)
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
        [HttpGet("{tenantName}")]
        public async Task<ActionResult<XR50Tenant>> GetXR50Tenant(string tenantName)
        {
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);

            if (XR50Tenant == null)
            {
                return NotFound($"Could not Find Tenant {tenantName}");
            }

            return XR50Tenant;
        }
        // GET: api/XR50Tenant/5
      /*  [HttpGet("{tenantName}/trainingPrograms")]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetTenantTrainingPrograms(string tenantName)
        {
            return _context.TrainingPrograms.Where(t=>t.TenantName.Equals(tenantName)).ToList();
        }
        [HttpGet("{tenantName}/materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetTenantMaterials(string tenantName)
        {
            return _context.Materials.Where(t=>t.TenantName.Equals(tenantName)).ToList();
        }

        // GET: api/XR50Tenant/5
*/
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
            values.Add(new KeyValuePair<string, string>("groupid", XR50Tenant.TenantGroup));
            FormUrlEncodedContent messageContent = new FormUrlEncodedContent(values);
            string username = _configuration.GetValue<string>("TenantSettings:Admin");
            string password = _configuration.GetValue<string>("TenantSettings:Password");
            string uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("TenantSettings:GroupsPath");
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
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
            valuesAdmin.Add(new KeyValuePair<string, string>("groups[]", XR50Tenant.TenantGroup));
            //Target The User Interface
            uri_path = _configuration.GetValue<string>("TenantSettings:UsersPath");
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
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.TenantDirectory);
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
     /*   [HttpPost("/xr50/trainingAssetRepository/[controller]/trainingPrograms/{tenantName}")]
        public async Task<ActionResult<TrainingProgram>> PostTrainingProgram(string tenantName,TrainingProgram TrainingProgram)
        {
	        if (!tenantName.Equals(TrainingProgram.TenantName)) {
		        return NotFound($"Missmatch beteween {TrainingProgram.TenantName} and {tenantName}");
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

            XR50Tenant.TrainingProgramList.Add(TrainingProgram.ProgramName ); 
            _context.TrainingPrograms.Add(TrainingProgram);
            await _context.SaveChangesAsync();

           
            return CreatedAtAction("PostTrainingProgram", TrainingProgram);
        }
        [HttpPost("/xr50/trainingAssetRepository/[controller]/materials/{tenantName}/{parentMaterialId}")]
        public async Task<ActionResult<Material>> PostChildMaterial(string tenantName, string parentMaterialId, Material Material)
        {

            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Material.TenantName}");
            }
           
            var ParentMaterial = await _context.Materials.FindAsync(parentMaterialId);
            if (ParentMaterial == null) {
                return NotFound($"Couldnt Find Material with Id: {parentMaterialId}");
            }
            Material.MaterialId = Guid.NewGuid().ToString();
            
            Material.ParentId =ParentMaterial.MaterialId;
            ParentMaterial.MaterialList.Add(Material.MaterialId);
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostChildMaterial", tenantName, Material);
        }
        //POST api/LearningPath/tennantName/ProgramName
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
       [HttpPost("/xr50/trainingAssetRepository/[controller]/learningPaths/{tenantName}/{programName}")]
        public async Task<ActionResult<LearningPath>> PostLearningPath(string tenantName, string programName, LearningPath LearningPath)
        {
	       
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var XR50TrainingProgram = await _context.TrainingPrograms.FindAsync(tenantName, programName);
            if (XR50TrainingProgram == null)
            {
                return NotFound($"Couldnt Find Training Program {programName}");
            }

            
            LearningPath.LearningPathId = Guid.NewGuid().ToString();
            XR50TrainingProgram.LearningPathList.Add(LearningPath.LearningPathId);
 
            _context.LearningPaths.Add(LearningPath);
            await _context.SaveChangesAsync();

           
            return CreatedAtAction("PostLearningPath", LearningPath);
        }*/
        // DELETE: api/XR50Tenant/5
        [HttpDelete("{tenantName}")]
        public async Task<IActionResult> DeleteXR50Tenant(string tenantName)
        {
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                Console.WriteLine($"Did not find XR app with id: {tenantName}");
                return NotFound();
            }
            var adminUser = await _context.Users.FindAsync(XR50Tenant.OwnerName); 
	        if (adminUser == null) {
		        Console.WriteLine($"Did not find Owner: {XR50Tenant.OwnerName}");
                return NotFound();
            }

            foreach (string programName in XR50Tenant.TrainingProgramList) {
              var training= await _context.TrainingPrograms.FindAsync(tenantName,programName);
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
            string username = _configuration.GetValue<string>("TenantSettings:Admin");
            string password = _configuration.GetValue<string>("TenantSettings:Password");
            string uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
            string uri_group = _configuration.GetValue<string>("TenantSettings:GroupsPath");
	        string uri_user = _configuration.GetValue<string>("TenantSettings:UsersPath");
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");

            string authenticationString = $"{username}:{password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_group}/{XR50Tenant.TenantGroup}")
            {
                Content = messageContent
            };
            Console.WriteLine(XR50Tenant.TenantGroup);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            //_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            // Delete root dir for the Tenant
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.TenantDirectory);
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
       /* [HttpDelete("/xr50/trainingAssetRepository/[controller]/trainingPrograms/{tenantName}/{programName}")]
        public async Task<IActionResult> DeleteTrainingProgram(string tenantName,string programName )
        {
            var TrainingProgram = await _context.TrainingPrograms.FindAsync(tenantName,programName );
            if (TrainingProgram == null)           
            {
                return NotFound($"Did not find training {programName }");
            }

            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
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
            XR50Tenant.TrainingProgramList.Remove(TrainingProgram.ProgramName );
            await _context.SaveChangesAsync();

            //Tenant stuff
            string username = admin.UserName;
            string password = admin.Password;
            string uri_base = _configuration.GetValue<string>("TenantSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("TenantSettings:GroupsPath");
            string webdav_base = _configuration.GetValue<string>("TenantSettings:BaseWebDAV");
            
            // Remove root dir for the TrainingProgram
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tenant.TenantDirectory);
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{dirl}/{TrainingProgram.ProgramName }\"";
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
        [HttpDelete("/xr50/trainingAssetRepository/[controller]/materials/{tenantName}/{programName}/{materialId}")]
        public async Task<IActionResult> DeleteMaterial(string tenantName, string programName , string materialId)
        {
            var Material = await _context.Materials.FindAsync(materialId);
            if (Material == null)
            {
                return NotFound();
            }

            _context.Materials.Remove(Material);
            await _context.SaveChangesAsync();

	        var TrainingProgram = _context.TrainingPrograms.FirstOrDefault(t=> t.ProgramName .Equals(programName ) && t.TenantName.Equals(tenantName));
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
        }*/
        private bool XR50TenantExists(string tenantName)
        {
            return _context.Tenants.Any(e => e.TenantName.Equals(tenantName));
        }
    }
}
