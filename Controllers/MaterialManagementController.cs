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
using XR50TrainingAssetRepo.Models;
using System.ComponentModel.DataAnnotations;

namespace XR50TrainingAssetRepo.Controllers
{

    //[Route("/xr50/trainingAssetRepository/[controller]")]
    [Route("xr50/trainingAssetRepository/tenants/{tenantName}/[controller]")]
    [ApiController]
    public class materialsController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        IConfiguration _configuration;  
        public materialsController(XR50TrainingAssetRepoContext context,HttpClient httpClient, IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _configuration = configuration; 
        }

        // GET: api/Materialss
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterial(string tenantName)
        {
            return _context.Materials.Where(t=>t.TenantName.Equals(tenantName)).ToList();
        }

        // GET: api/Materialss/5
        [HttpGet("{materialId}")]
        public async Task<ActionResult<Material>> GetMaterials(string materialId)
        {
            var Material = await _context.Materials.FindAsync(materialId);

            if (Material == null)
            {
                return NotFound();
            }

            return Material;
        }
        // GET: /xr50/trainingAssetRepository/material_management/workflow/{tenantName}
        [HttpGet("workflow")]
        public async Task<ActionResult<IEnumerable<WorkflowMaterial>>> GetWorkflowMaterialsByTenant(string tenantName)
        {
            var workflows =_context.Workflows.Where(e => e.TenantName.Equals(tenantName)).ToList();
             if (workflows == null) {
                return NotFound("No workflows found");
            }
            foreach (var workflow in workflows) {
                foreach (var stepId in workflow.StepIds) {
                    var step = await _context.WorkflowSteps.FindAsync(stepId);
                    workflow.Steps.Add(step);
                }
            }
            return workflows;
        }
        // GET: /xr50/trainingAssetRepository/material_management/workflow/{tenantName}/{materialId}
        [HttpGet("workflow/{materialId}")]
        public async Task<ActionResult<WorkflowMaterial>> GetWorkflowMaterial(string tenantName, string materialId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantName);
            if (tenant == null)
            {
                return NotFound($"Tenant {tenantName} not found");
            }

            var material = await _context.Workflows.FindAsync(materialId);            

            if (material == null)
            {
                return NotFound($"Workflow material with ID {materialId} not found for tenant {tenantName}");
            }

            return material;
        }

        
        [HttpGet("checklist/{materialId}")]
        public async Task<ActionResult<ChecklistMaterial>> GetChecklistMaterial(string tenantName, string materialId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantName);
            if (tenant == null)
            {
                return NotFound($"Tenant {tenantName} not found");
            }

            var material = await _context.Checklists.FindAsync(materialId);            

            if (material == null)
            {
                return NotFound($"Workflow material with ID {materialId} not found for tenant {tenantName}");
            }

            return material;
        }
        [HttpGet("image")]
        public async Task<ActionResult<ImageMaterial>> GetImageMaterial(string tenantName, string materialId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantName);
            if (tenant == null)
            {
                return NotFound($"Tenant {tenantName} not found");
            }

            var material = await _context.Images.FindAsync(materialId);            

            if (material == null)
            {
                return NotFound($"Image material with ID {materialId} not found for tenant {tenantName}");
            }

            return material;
        }
        [HttpGet("pdf")]
        public async Task<ActionResult<PDFMaterial>> GetPdfMaterial(string tenantName, string materialId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantName);
            if (tenant == null)
            {
                return NotFound($"Tenant {tenantName} not found");
            }

            var material = await _context.PDFs.FindAsync(materialId);            

            if (material == null)
            {
                return NotFound($"Image material with ID {materialId} not found for tenant {tenantName}");
            }

            return material;
        }
        [HttpGet("video")]
        public async Task<ActionResult<VideoMaterial>> GetVideoMaterial(string tenantName, string materialId)
        {
            var tenant = await _context.Tenants.FindAsync(tenantName);
            if (tenant == null)
            {
                return NotFound($"Tenant {tenantName} not found");
            }

            var material = await _context.Videos.FindAsync(materialId);            

            if (material == null)
            {
                return NotFound($"Video material with ID {materialId} not found for tenant {tenantName}");
            }

            return material;
        }
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterials(string tenantName, Material Material)
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
            return CreatedAtAction("PostMaterials",tenantName, Material);
        }
        
       /* [HttpPost("/xr50/trainingAssetRepository/[controller]/{tenantName}/{parentMaterialId}")]
        public async Task<ActionResult<Material>> PostChildMaterials(string tenantName, string parentMaterialId, Material Material)
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

            Material.ParentId=parentMaterialId;
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            await _context.SaveChangesAsync();
            return CreatedAtAction("PostMaterials",tenantName, Material);
        }
*/
        [HttpPost("workflow")]
        public async Task<ActionResult<Material>> PostWorkflowMaterial(string tenantName, WorkflowMaterial workflowMaterial)
        {

           /* Material Material = new Material();
            Material.MaterialType = MaterialType.Workflow;
            Material.Name = workflowMaterial.Name;
            Material.ParentId = workflowMaterial.ParentId;
            Material.TenantName = workflowMaterial.TenantName;
            Material.TrainingProgramList = workflowMaterial.TrainingProgramList;*/
            workflowMaterial.MaterialId= Guid.NewGuid().ToString();
          
            workflowMaterial.Steps.ForEach(step => {
                step.WorkflowStepId = Guid.NewGuid().ToString();
                _context.WorkflowSteps.Add(step);
            });
            //SaVE IN db to trigger autoincrement
            await _context.SaveChangesAsync();
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {tenantName}");
            }
             
           // _context.Materials.Add(Material);
            _context.Workflows.Add(workflowMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostWorkflowMaterial", tenantName, workflowMaterial);
        }
        
        [HttpPost("checklist")]
        public async Task<ActionResult<Material>> PostChecklistMaterial(string tenantName, ChecklistMaterial checklistMaterial)
        {
            /*Material Material = new Material();
            Material.MaterialType = MaterialType.Checklist;
            Material.Name = checklistMaterial.Name;
            Material.ParentId = checklistMaterial.ParentId;
            Material.TenantName = checklistMaterial.TenantName;
            Material.TrainingProgramList = checklistMaterial.TrainingProgramList;*/
            checklistMaterial.MaterialId = Guid.NewGuid().ToString();
            checklistMaterial.Entries.ForEach(entry => {
                entry.ChecklistEntryId = Guid.NewGuid().ToString();
                _context.ChecklistEntries.Add(entry);
            });

            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {checklistMaterial.TenantName}");
            }
        
            _context.Checklists.Add(checklistMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostChecklistMaterial", tenantName, checklistMaterial);
        }
        [HttpPost("image")]
        public async Task<ActionResult<ImageMaterial>> PostImageMaterial(string tenantName, ImageMaterial imageMaterial)
        {
          /*  Material Material = new Material();
            Material.MaterialType = MaterialType.Image;
            Material.Name = imageMaterial.Name;
            Material.TenantName = imageMaterial.TenantName;
            Material.ParentId = imageMaterial.ParentId;*/
            imageMaterial.MaterialId=Guid.NewGuid().ToString();
            
            if (imageMaterial.AssetId != null) {
                var Asset= await _context.Assets.FindAsync(imageMaterial.AssetId);
                if (Asset == null)
                {
                    return NotFound($"Couldnt Find Asset {imageMaterial.AssetId}");
                }
                Asset.MaterialList.Add(imageMaterial.MaterialId);
                
            }
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {imageMaterial.TenantName}");
            }
            
            _context.Materials.Add(imageMaterial);
            _context.Images.Add(imageMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostImageMaterial", tenantName, imageMaterial);
        }
        [HttpPost("pdf")]
        public async Task<ActionResult<PDFMaterial>> PostPdfMaterial(string tenantName, PDFMaterial pdfMaterial)
        {
          /*  Material Material = new Material();
            Material.MaterialType = MaterialType.Image;
            Material.Name = imageMaterial.Name;
            Material.TenantName = imageMaterial.TenantName;
            Material.ParentId = imageMaterial.ParentId;*/
            pdfMaterial.MaterialId= Guid.NewGuid().ToString();
            
            if (pdfMaterial.AssetId != null) {
                var Asset= await _context.Assets.FindAsync(pdfMaterial.AssetId);
                if (Asset == null)
                {
                    return NotFound($"Couldnt Find Asset {pdfMaterial.AssetId}");
                }
                Asset.MaterialList.Add(pdfMaterial.MaterialId);
                
            }
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {pdfMaterial.TenantName}");
            }
            
            _context.Materials.Add(pdfMaterial);
            _context.PDFs.Add(pdfMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostPdfMaterial", tenantName, pdfMaterial);
        }
                [HttpPost("pdf")]
        public async Task<ActionResult<UnityDemoMaterial>> PostUnityDemoMaterial(string tenantName, UnityDemoMaterial unityDemoMaterial)
        {
          /*  Material Material = new Material();
            Material.MaterialType = MaterialType.Image;
            Material.Name = imageMaterial.Name;
            Material.TenantName = imageMaterial.TenantName;
            Material.ParentId = imageMaterial.ParentId;*/
            unityDemoMaterial.MaterialId= Guid.NewGuid().ToString();
            
            if (unityDemoMaterial.AssetId != null) {
                var Asset= await _context.Assets.FindAsync(unityDemoMaterial.AssetId);
                if (Asset == null)
                {
                    return NotFound($"Couldnt Find Asset {unityDemoMaterial.AssetId}");
                }
                Asset.MaterialList.Add(unityDemoMaterial.MaterialId);
                
            }
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {unityDemoMaterial.TenantName}");
            }
            
            _context.Materials.Add(unityDemoMaterial);
            _context.Demos.Add(unityDemoMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostUnityDemiMaterial", tenantName, unityDemoMaterial);
        }
        [HttpPost("video")]
        public async Task<ActionResult<Material>> PostVideoMaterial(string tenantName, VideoMaterial videoMaterial)
        {
           /* Material Material = new Material();
            Material.MaterialType = MaterialType.Video;
            Material.Name = videoMaterial.Name;
            Material.ParentId = videoMaterial.ParentId;
            Material.TrainingProgramList = videoMaterial.TrainingProgramList;*/
            videoMaterial.MaterialId = videoMaterial.MaterialId;
            videoMaterial.Timestamps.ForEach(timestamp=> {
                timestamp.VideoTimestampId = Guid.NewGuid().ToString();
                _context.VideoTimestamps.Add(timestamp);
            });
            if (videoMaterial.AssetId != null) {
                var Asset= await _context.Assets.FindAsync(videoMaterial.AssetId);
                if (Asset == null)
                {
                    return NotFound($"Couldnt Find Asset {videoMaterial.AssetId}");
                }
                 Asset.MaterialList.Add(videoMaterial.MaterialId);
                
            }
            var XR50Tenant = await _context.Tenants.FindAsync(tenantName);
            if (XR50Tenant == null)
            {
                return NotFound($"Couldnt Find Tenant {tenantName}");
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Couldnt Find Admin user for {videoMaterial.TenantName}");
            }
            
            
            _context.Videos.Add(videoMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostVideoMaterial", tenantName, videoMaterial);
        }
       /* // PUT: api/Materialss/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{MaterialId}")]
        public async Task<IActionResult> PutMaterials(string MaterialId, Material Material)
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
                if (!MaterialsExists(MaterialId))
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
        // DELETE: api/Materialss/5
        [HttpDelete("{materialId}")]
        public async Task<IActionResult> DeleteMaterialById(string materialId)
        {
            var Material = await _context.Materials.FindAsync(materialId);
            if (Material == null)
            {
                return NotFound();
            }

            foreach (string trainingId in Material.TrainingProgramList) {

	            var TrainingProgram = await _context.TrainingPrograms.FindAsync(Material.TenantName,trainingId);
                if (TrainingProgram == null)
                {
                    return NotFound();
                }
	            TrainingProgram.MaterialList.Remove(materialId);
            }
            var XR50Tenant = await _context.Tenants.FindAsync(Material.TenantName);
            if (XR50Tenant == null)
            {
                return NotFound();
            }
            var admin = await _context.Users.FindAsync(XR50Tenant.OwnerName);
            if (admin == null)
            {
                return NotFound($"Admin user for {Material.TenantName}");
            }
            _context.Materials.Remove(Material);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool MaterialsExists(string Name)
        {
            return _context.Materials.Any(e => e.Name.Equals(Name));
        }
        
    }


}
