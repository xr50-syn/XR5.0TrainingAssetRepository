 using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Controllers
{
    
    [Route("/xr50/library_of_reality_altering_knowledge/[controller]")]
    [ApiController]
    public class material_managementController : ControllerBase
    {
        private readonly XR50RepoContext _context;
        private readonly HttpClient _httpClient;
        IConfiguration _configuration;  
        public material_managementController(XR50RepoContext context,HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 
        }

        // GET: api/MaterialManagements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterial()
        {
            return await _context.Materials.ToListAsync();
        }

        // GET: api/MaterialManagements/5
        [HttpGet("{MaterialId}")]
        public async Task<ActionResult<Material>> GetMaterialManagement(string MaterialId)
        {
            var Material = await _context.Materials.FindAsync(MaterialId);

            if (Material == null)
            {
                return NotFound();
            }

            return Material;
        }
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}")]
        public async Task<ActionResult<Material>> PostMaterialManagement(string TennantName, Material Material)
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
            switch (Material.MaterialType) {
                case MaterialType.Checklist:

                break;
                case MaterialType.Image:

                break;
                case MaterialType.Workflow:

                break;
                case MaterialType.Video:

                break;

            } 
            
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostMaterialManagement",TennantName, Material);
        }
        
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}/{ParentMaterialId}")]
        public async Task<ActionResult<Material>> PostChildMaterialManagement(string TennantName, string ParentMaterialId, Material Material)
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
            switch (Material.MaterialType) {
                case MaterialType.Checklist:

                break;
                case MaterialType.Image:

                break;
                case MaterialType.Workflow:

                break;
                case MaterialType.Video:

                break;

            } 

            Material.ParentId=ParentMaterialId;
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            
            string username = admin.UserName;
            string password = admin.Password;
            string webdav_base = _configuration.GetValue<string>("OwncloudSettings:BaseWebDAV");
            string MaterialPath= Material.MaterialName;
            
            // Createe root dir for the Training
            string cmd="curl";
            string dirl=System.Web.HttpUtility.UrlEncode(XR50Tennant.OwncloudDirectory);
            string Arg= $"-X MKCOL -u {username}:{password} \"{webdav_base}/{dirl}/{MaterialPath}\"";
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
            return CreatedAtAction("PostMaterialManagement",TennantName, Material);
        }

        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}/workflow")]
        public async Task<ActionResult<Material>> PostWorkflowMaterial(string TennantName, Material Material)
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
             
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostWorkflowMaterial", TennantName, Material);
        }
        
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}/checklist")]
        public async Task<ActionResult<Material>> PostChecklistMaterial(string TennantName, Material Material)
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
            
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostChecklistMaterial", TennantName, Material);
        }
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}/image")]
        public async Task<ActionResult<Material>> PostImageMaterial([FromForm] FileUploadFormData fileUpload)
        {
            Material material = new Material();
            material.TennantName=fileUpload.TennantName;
            if (fileUpload.TrainingName != null) {
                material.TrainingList.Add(fileUpload.TrainingName);
                var training= await _context.Trainings.FindAsync(fileUpload.TennantName,fileUpload.TrainingName);
                if (training == null) {
                    training.MaterialList.Add(material.MaterialId);
                }
            }
            if (fileUpload.ParentId != null) {
                material.ParentId = fileUpload.ParentId;
                var parent = await _context.Materials.FindAsync(fileUpload.ParentId);
                if (parent ==null) {
                    return NotFound($"Parent Material with {fileUpload.ParentId}");
                } else {
                    parent.MaterialList.Add(material.MaterialId);
                }
            }
            material.Description=fileUpload.Description;
            material.MaterialType= MaterialType.Image;
            var XR50Tennant = await _context.Tennants.FindAsync(material.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find Tennant {material.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {material.TennantName}");
            }
            Asset asset= new Asset();
            asset.Description=fileUpload.Description;
            asset.TennantName=fileUpload.TennantName;
            asset.OwncloudPath= fileUpload.OwncloudPath;
            
            if (fileUpload.Type != null) {
                asset.OwncloudFileName += $".{fileUpload.Type}";
            }
            _context.Assets.Add(asset);
            
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
            string Arg= $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{tempFileName}\" \"{webdav_base}/{dirl}/{asset.OwncloudFileName}\"";
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
            
            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostImageMaterial", material);
        }
        [HttpPost("/xr50/library_of_reality_altering_knowledge/[controller]/{TennantName}/video")]
        public async Task<ActionResult<Material>> PostVideoMaterial([FromForm] FileUploadFormData fileUpload)
        {
            Material material = new Material();
            material.TennantName=fileUpload.TennantName;
            if (fileUpload.TrainingName != null) {
                material.TrainingList.Add(fileUpload.TrainingName);
                var training= await _context.Trainings.FindAsync(fileUpload.TennantName,fileUpload.TrainingName);
                if (training == null) {
                    training.MaterialList.Add(material.MaterialId);
                }
            }
            if (fileUpload.ParentId != null) {
                material.ParentId = fileUpload.ParentId;
                var parent = await _context.Materials.FindAsync(fileUpload.ParentId);
                if (parent ==null) {
                    return NotFound($"Parent Material with {fileUpload.ParentId}");
                } else {
                    parent.MaterialList.Add(material.MaterialId);
                }
            }
            material.Description=fileUpload.Description;
            material.MaterialType= MaterialType.Video;
            var XR50Tennant = await _context.Tennants.FindAsync(material.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound($"Couldnt Find Tennant {material.TennantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {material.TennantName}");
            }
                        Asset asset= new Asset();
            asset.Description=fileUpload.Description;
            asset.TennantName=fileUpload.TennantName;
            asset.OwncloudPath= fileUpload.OwncloudPath;
            
            if (fileUpload.Type != null) {
                asset.OwncloudFileName += $".{fileUpload.Type}";
            }
            _context.Assets.Add(asset);
            
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
            string Arg= $"-X PUT -u {username}:{password} --cookie \"XDEBUG_SESSION=MROW4A;path=/;\" --data-binary @\"{tempFileName}\" \"{webdav_base}/{dirl}/{asset.OwncloudFileName}\"";
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

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostVideoMaterial", material);
        }
       /* // PUT: api/MaterialManagements/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{MaterialId}")]
        public async Task<IActionResult> PutMaterialManagement(string MaterialId, Material Material)
        {
            if (!MaterialId.Equals(Material.MaterialId))
            {
                return BadRequest();
            }

            _context.Entry(Material).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MaterialManagementExists(MaterialId))
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
        // DELETE: api/MaterialManagements/5
        [HttpDelete("{MaterialId}")]
        public async Task<IActionResult> DeleteMaterialById(string MaterialId)
        {
            var Material = await _context.Materials.FindAsync(MaterialId);
            if (Material == null)
            {
                return NotFound();
            }

            foreach (string trainingId in Material.TrainingList) {

	            var Training = await _context.Trainings.FindAsync(Material.TennantName,trainingId);
                if (Training == null)
                {
                    return NotFound();
                }
	            Training.MaterialList.Remove(MaterialId);
            }
            var XR50Tennant = await _context.Tennants.FindAsync(Material.TennantName);
            if (XR50Tennant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tennant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Material.TennantName}");
            }
            _context.Materials.Remove(Material);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool MaterialManagementExists(string MaterialName)
        {
            return _context.Materials.Any(e => e.MaterialName.Equals(MaterialName));
        }
    }
}
