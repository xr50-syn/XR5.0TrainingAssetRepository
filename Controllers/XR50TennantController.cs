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
using XR5_0TrainingRepo.Models;
using Microsoft.AspNetCore.Authorization;

namespace XR5_0TrainingRepo.Controllers
{

    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
    [ApiController]
    
    public class tennant_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
     
        public tennant_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration= configuration;
        }

        // GET: api/XR50Tennant
        [HttpGet]
        public async Task<ActionResult<IEnumerable<XR50Tennant>>> GetTennants()
        {
            return await _context.Tennants.ToListAsync();
        }
        
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}")]
        public async Task<ActionResult<XR50Tennant>> GetXR50Tennant(string TennantName)
        {
            var XR50Tennant = await _context.Tennants.FindAsync(TennantName);

            if (XR50Tennant == null)
            {
                return NotFound();
            }

            return XR50Tennant;
        }
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}/TrainingModules")]
        public async Task<ActionResult<IEnumerable<TrainingModule>>> GetTennantTrainings(string TennantName)
        {
            return _context.Trainings.Where(t=>t.TennantName.Equals(TennantName)).ToList();
        }
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}/{TrainingName}/Materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetTrainingMaterials(string TennantName,string TrainingName)
        {
            return  _context.Materials.Where(r=>r.TennantName.Equals(TennantName) && r.TrainingName.Equals(TrainingName)).ToList();
        }
        /*
        // PUT: api/XR50Tennant/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutXR50Tennant(long id, XR50Tennant XR50Tennant)
        {
            if (id != XR50Tennant.TennantId)
            {
                return BadRequest();
            }

            _context.Entry(XR50Tennant).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!XR50TennantExists(id))
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

        // POST: api/XR50Tennant
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<XR50Tennant>> PostXR50Tennant(XR50Tennant XR50Tennant)
        {

            _context.Tennants.Add(XR50Tennant);

            var values = new List<KeyValuePair<string, string>>();
            values.Add(new KeyValuePair<string, string>("groupid", XR50Tennant.OwncloudGroup));
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
            User adminUser = XR50Tennant.Owner;
            XR50Tennant.OwnerName = adminUser.UserName;
	        XR50Tennant.AdminList.Add(adminUser.UserName);
            _context.Users.Add(adminUser);
            _context.SaveChanges();
            //Console.WriteLine($"Response content: {resultContent}");
            //Create the admin User
            var valuesAdmin = new List<KeyValuePair<string, string>>();
            valuesAdmin.Add(new KeyValuePair<string, string>("userid", adminUser.UserName));
            valuesAdmin.Add(new KeyValuePair<string, string>("password", adminUser.Password));
            valuesAdmin.Add(new KeyValuePair<string, string>("email", adminUser.UserEmail));
            valuesAdmin.Add(new KeyValuePair<string, string>("display", adminUser.FullName));
            valuesAdmin.Add(new KeyValuePair<string, string>("groups[]", XR50Tennant.OwncloudGroup));
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

            // Create root dir for the Tennant, owned by Admin
	        string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{dirl}/\"";
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
            _context.SaveChanges();
            return CreatedAtAction("PostXR50Tennant", XR50Tennant);
        }
        // POST: api/XR50Tennant/
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/training-management/{TennantName}")]
        public async Task<ActionResult<TrainingModule>> PostTraining(string TennantName,TrainingModule Training)
        {
	        if (!TennantName.Equals(Training.TennantName)) {
		        return NotFound($"Couldnt Find Tennant {Training.TennantName} is not our parent");
	        }
            var XR50Tennant = await _context.Tennants.FindAsync(Training.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find Tennant {Training.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin ==null) 
            {
                return NotFound($"Couldnt Find Admin user for {Training.TennantName}");
            }
            Training.TrainingId= Guid.NewGuid().ToString();; 
                
            XR50Tennant.TrainingList.Add(Training.TrainingId); 
            _context.Trainings.Add(Training);
            await _context.SaveChangesAsync();

           
            return CreatedAtAction("PostTraining", Training);
        }
        // POST: api/XR50Tennant
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/material-management/{TennantName}/{TrainingName}")]
        public async Task<ActionResult<Material>> PostMaterial(string TennantName, string TrainingName, Material Material)
        {
            var XR50Tennant = await _context.Tennants.FindAsync(Material.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find Tennant {Material.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Material.TennantName}");
            }
            var Training = await _context.Trainings.FindAsync(TennantName,TrainingName);
            if (Training == null)
            {
                return NotFound($"Couldnt Find Training for {Material.TrainingName}");
            }
            Material.MaterialId = Guid.NewGuid().ToString();
            Training.MaterialList.Add(Material.MaterialId);
            _context.Materials.Add(Material);
            Material.ParentId=Training.TrainingId;
            await _context.SaveChangesAsync();
           
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{dirl}/{Training.TrainingName}/{Material.OwncloudFileName}\"";
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
            
            _context.SaveChanges();
            return CreatedAtAction("PostMaterial", TrainingName, Material);
        }

        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/material-management/{TennantName}/{TrainingName}/{ParentMaterialId}")]
        public async Task<ActionResult<Material>> PostMaterialManagement(string TennantName, string TrainingName, string ParentMaterialId, Material Material)
        {

            var XR50Tennant = await _context.Tennants.FindAsync(TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find Tennant {TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Material.TennantName}");
            }
            var Training = await _context.Trainings.FindAsync(TennantName,TrainingName);
            if (Training == null)
            {
                return NotFound($"Couldnt Find Training for {Material.TrainingName}");
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
            
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string MaterialPath=ParentMaterial.MaterialName + "/"+ Material.MaterialName;
        
            // Create root dir for the Training
            string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{dirl}/{Training.TrainingName}/{MaterialPath}\"";
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
            
            _context.SaveChanges();
            return CreatedAtAction("PostMaterialManagement", TennantName, TrainingName, Material);
        }

        // DELETE: api/XR50Tennant/5
        [HttpDelete("{TennantName}")]
        public async Task<IActionResult> DeleteXR50Tennant(string TennantName)
        {
            var XR50Tennant = await _context.Tennants.FindAsync(TennantName);
            if (XR50Tennant == null)
            {
                Console.WriteLine($"Did not find XR app with id: {TennantName}");
                return NotFound();
            }
            var adminUser = await _context.Users.FindAsync(XR50Tennant.OwnerName); 
	        if (adminUser == null) {
		        Console.WriteLine($"Did not find Owner: {XR50Tennant.OwnerName}");
                return NotFound();
            }

            foreach (string trainingId in XR50Tennant.TrainingList) {
              var training= await _context.Trainings.FindAsync(trainingId);
              foreach (string resourceId in training.MaterialList) {
                var resource= await _context.Materials.FindAsync(resourceId);
                _context.Materials.Remove(resource);
              }     
              _context.Trainings.Remove(training);
            }
            _context.Tennants.Remove(XR50Tennant);
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
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_group}/{XR50Tennant.OwncloudGroup}")
            {
                Content = messageContent
            };
            Console.WriteLine(XR50Tennant.OwncloudGroup);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
            _httpClient.BaseAddress = new Uri(uri_base);
            //_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64EncodedAuthenticationString}");
            var result = _httpClient.SendAsync(request).Result;
            string resultContent = result.Content.ReadAsStringAsync().Result;
            // Delete root dir for the Tennant
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
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
            request = new HttpRequestMessage(HttpMethod.Delete, $"{uri_user}/{XR50Tennant.OwnerName}")
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
        [HttpDelete("/xr50/library_of_reality_altering_knowledge/[controller]/training-management/{TennantName}/{TrainingName}")]
        public async Task<IActionResult> DeleteTraining(string TennantName,string TrainingName)
        {
            var Training = await _context.Trainings.FindAsync(TennantName,TrainingName);
            if (Training == null)           
            {
                return NotFound($"Did not find training {TrainingName}");
            }

            var XR50Tennant = await _context.Tennants.FindAsync(TennantName);
            if (XR50Tennant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Training.TennantName}");
            }
            foreach (string resourceId in Training.MaterialList) {
                var resource= await _context.Materials.FindAsync(resourceId);
                _context.Materials.Remove(resource);
              }     
            _context.Trainings.Remove(Training);
            XR50Tennant.TrainingList.Remove(Training.TrainingId);
            await _context.SaveChangesAsync();

            //Owncloud stuff
            string username = admin.UserName;
            string password = admin.Password;
            string uri_base = _configuration.GetValue<string>("OwncloudSettings:BaseAPI");
            string uri_path = _configuration.GetValue<string>("OwncloudSettings:GroupManagementPath");
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            
            // Remove root dir for the Training
	        string cmd= "curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{dirl}/{Training.TrainingName}\"";
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


        // DELETE: api/XR50Tennant/
        [HttpDelete("/xr50/library_of_reality_altering_knowledge/[controller]/material-management/{TennantName}/{TrainingName}/{MaterialName}")]
        public async Task<IActionResult> DeleteMaterial(string TennantName, string TrainingName, string MaterialName)
        {
            var Material = _context.Materials.FirstOrDefault( r=> r.MaterialName.Equals(MaterialName) && r.TrainingName.Equals(TrainingName) && r.TennantName.Equals(TennantName));
            if (Material == null)
            {
                return NotFound();
            }

            _context.Materials.Remove(Material);
            await _context.SaveChangesAsync();

	        var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(TrainingName) && t.TennantName.Equals(TennantName));
            if (Training == null)
            {
                return NotFound();
            }
	        Training.MaterialList.Remove(Material.MaterialId);
            var XR50Tennant = await _context.Tennants.FindAsync(Training.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Training.TennantName}");
            }
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	        string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X DELETE -u {username}:{password} \"{webdav_base}/{dirl}/{Training.TrainingName}/{Material.OwncloudFileName}\"";
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
            return NoContent();
        }
        private bool XR50TennantExists(string TennantName)
        {
            return _context.Tennants.Any(e => e.TennantName.Equals(TennantName));
        }
    }
}
