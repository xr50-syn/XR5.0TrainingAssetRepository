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
    
    public class app_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
     
        public app_managementController(XR50RepoContext context, HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration= configuration;
        }

        // GET: api/XR50Tennant
        [HttpGet]
        public async Task<ActionResult<IEnumerable<XR50Tennant>>> GetApps()
        {
            return await _context.Apps.ToListAsync();
        }
        
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}")]
        public async Task<ActionResult<XR50Tennant>> GetXR50Tennant(string TennantName)
        {
            var XR50Tennant = await _context.Apps.FindAsync(TennantName);

            if (XR50Tennant == null)
            {
                return NotFound();
            }

            return XR50Tennant;
        }
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}/TrainingModules")]
        public async Task<ActionResult<IEnumerable<TrainingModule>>> GetAppTrainings(string TennantName)
        {
            return _context.Trainings.Where(t=>t.TennantName.Equals(TennantName)).ToList();
        }
        // GET: api/XR50Tennant/5
        [HttpGet("{TennantName}/{trainingName}/Materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetTrainingResources(string TennantName,string trainingName)
        {
            return  _context.Resources.Where(r=>r.TennantName.Equals(TennantName) && r.TrainingName.Equals(trainingName)).ToList();
        }
        /*
        // PUT: api/XR50Tennant/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutXR50Tennant(long id, XR50Tennant XR50Tennant)
        {
            if (id != XR50Tennant.AppId)
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

            _context.Apps.Add(XR50Tennant);

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

            // Create root dir for the App, owned by Admin
	        string cmd="curl";
            string Arg= $"-X MKCOL -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/\"";
            // Create root dir for the App
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
		        return NotFound($"Couldnt Find App {Training.TennantName} is not our parent");
	        }
            var XR50Tennant = await _context.Apps.FindAsync(Training.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find App {Training.TennantName}");
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

            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
	        string cmd="curl";
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}\"";
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
            return CreatedAtAction("PostTraining", Training);
        }
        // POST: api/XR50Tennant
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/resource-management/{TennantName}/{TrainingName}")]
        public async Task<ActionResult<Material>> PostResourceManagement(string TennantName, string TrainingName, Material Material)
        {

            var XR50Tennant = await _context.Apps.FindAsync(Material.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find App {Material.TennantName}");
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
            Material.ResourceId = Guid.NewGuid().ToString();
            Training.ResourceList.Add(Material.ResourceId);
            _context.Resources.Add(Material);
            Material.ParentType = "TRAINING";
            await _context.SaveChangesAsync();
           
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            // Createe root dir for the Training
            string cmd="curl";
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}/{Material.OwncloudFileName}\"";
            // Create root dir for the App
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
            return CreatedAtAction("PostResourceManagement", Material);
        }

         [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/resource-management/{TennantName}/{TrainingName}/{ParentResourceId}")]
        public async Task<ActionResult<Material>> PostResourceManagement(string TennantName, string TrainingName, string ParentResourceId, Material Material)
        {

            var XR50Tennant = await _context.Apps.FindAsync(TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find App {TennantName}");
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
            var ParentResource = await _context.Resources.FindAsync(ParentResourceId);
            if (ParentResource == null) {
                return NotFound($"Couldnt Find Resource with Id: {ParentResourceId}");
            }
            Material.ResourceId = Guid.NewGuid().ToString();
            Material.ParentType = "RESOURCE";
            Material.ParentId =ParentResource.ResourceId;
            ParentResource.ResourceList.Add(Material.ResourceId);
            _context.Resources.Add(Material);
            await _context.SaveChangesAsync();
            
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string ResourcePath=ParentResource.ResourceName + "/"+ Material.ResourceName;
            while (ParentResource.ParentType.Equals("RESOURCE")) {
                ResourcePath= ParentResource.ResourceName + "/" + ResourcePath;
                ParentResource = await _context.Resources.FindAsync(ParentResource.ParentId);
            }
            // Createe root dir for the Training
            string cmd="curl";
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}/{ResourcePath}\"";
            // Create root dir for the App
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
            return CreatedAtAction("PostResourceManagement", Material);
        }

        // DELETE: api/XR50Tennant/5
        [HttpDelete("{TennantName}")]
        public async Task<IActionResult> DeleteXR50Tennant(string TennantName)
        {
            var XR50Tennant = await _context.Apps.FindAsync(TennantName);
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
              foreach (string resourceId in training.ResourceList) {
                var resource= await _context.Resources.FindAsync(resourceId);
                _context.Resources.Remove(resource);
              }     
              _context.Trainings.Remove(training);
            }
            _context.Apps.Remove(XR50Tennant);
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
            // Delete root dir for the App
	        string cmd= "curl";
            string Arg=  $"-X DELETE -u {adminUser.UserName}:{adminUser.Password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/\"";
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

            var XR50Tennant = await _context.Apps.FindAsync(TennantName);
            if (XR50Tennant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {Training.TennantName}");
            }
            foreach (string resourceId in Training.ResourceList) {
                var resource= await _context.Resources.FindAsync(resourceId);
                _context.Resources.Remove(resource);
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
            string Arg=  $"-X DELETE -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}\"";
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
        [HttpDelete("/xr50/library_of_reality_altering_knowledge/[controller]/resource-management/{TennantName}/{TrainingName}/{ResourceName}")]
        public async Task<IActionResult> DeleteMaterial(string TennantName, string TrainingName, string ResourceName)
        {
            var Material = _context.Resources.FirstOrDefault( r=> r.ResourceName.Equals(ResourceName) && r.TrainingName.Equals(TrainingName) && r.TennantName.Equals(TennantName));
            if (Material == null)
            {
                return NotFound();
            }

            _context.Resources.Remove(Material);
            await _context.SaveChangesAsync();

	        var Training = _context.Trainings.FirstOrDefault(t=> t.TrainingName.Equals(TrainingName) && t.TennantName.Equals(TennantName));
            if (Training == null)
            {
                return NotFound();
            }
	        Training.ResourceList.Remove(Material.ResourceId);
            var XR50Tennant = await _context.Apps.FindAsync(Training.TennantName);
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
            string Arg= $"-X DELETE -u {username}:{password} \"{webdav_base}/{XR50Tennant.OwncloudDirectory}/{Training.TrainingName}/{Material.OwncloudFileName}\"";
            // Create root dir for the App
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
            return _context.Apps.Any(e => e.TennantName.Equals(TennantName));
        }
    }
}
