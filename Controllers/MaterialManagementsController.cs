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
    
    [Route("/xr50/TrainingProgram_Asset_Repository/[controller]")]
    [ApiController]
    public class material_managementController : ControllerBase
    {
        private readonly XR50TrainingAssetRepoContext _context;
        private readonly HttpClient _httpClient;
        IConfiguration _configuration;  
        public material_managementController(XR50TrainingAssetRepoContext context,HttpClient httpClient, IConfiguration configuration)
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
        // GET: /xr50/TrainingProgram_Asset_Repository/material_management/workflow/{tenantName}
        [HttpGet("workflow/{tenantName}")]
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
        // GET: /xr50/TrainingProgram_Asset_Repository/material_management/workflow
        [HttpGet("workflow")]
        public async Task<ActionResult<IEnumerable<WorkflowMaterial>>> GetWorkflowMaterials()
        {
            var workflows = await _context.Workflows.ToListAsync();
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
        // GET: /xr50/TrainingProgram_Asset_Repository/material_management/workflow/{tenantName}/{materialId}
        [HttpGet("workflow/{tenantName}/{materialId}")]
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

        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}")]
        public async Task<ActionResult<Material>> PostMaterialManagement(string TenantName, Material Material)
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
            return CreatedAtAction("PostMaterialManagement",TenantName, Material);
        }
        
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}/{ParentMaterialId}")]
        public async Task<ActionResult<Material>> PostChildMaterialManagement(string TenantName, string ParentMaterialId, Material Material)
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
            return CreatedAtAction("PostMaterialManagement",TenantName, Material);
        }

        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}/workflow")]
        public async Task<ActionResult<Material>> PostWorkflowMaterial(string TenantName, WorkflowMaterial workflowMaterial)
        {

            Material Material = new Material();
            Material.MaterialType = MaterialType.Workflow;
            Material.MaterialName = workflowMaterial.MaterialName;
            Material.ParentId = workflowMaterial.ParentId;
            Material.TrainingProgramList = workflowMaterial.TrainingProgramList;
            Material.MaterialId = workflowMaterial.MaterialId;

            workflowMaterial.Steps.ForEach(step => {
                step.WorkflowStepId = Guid.NewGuid().ToString();
                _context.WorkflowSteps.Add(step);
            });
            //SaVE IN db to trigger autoincrement
            await _context.SaveChangesAsync();
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
             
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            _context.Workflows.Add(workflowMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostWorkflowMaterial", TenantName, Material);
        }
        
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}/checklist")]
        public async Task<ActionResult<Material>> PostChecklistMaterial(string TenantName, ChecklistMaterial checklistMaterial)
        {
            Material Material = new Material();
            Material.MaterialType = MaterialType.Checklist;
            Material.MaterialName = checklistMaterial.MaterialName;
            Material.ParentId = checklistMaterial.ParentId;
            Material.TrainingProgramList = checklistMaterial.TrainingProgramList;
            Material.MaterialId = checklistMaterial.MaterialId;
            checklistMaterial.Entries.ForEach(entry => {
                entry.ChecklistEntryId = Guid.NewGuid().ToString();
                _context.ChecklistEntries.Add(entry);
            });

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
            
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            _context.Checklists.Add(checklistMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostChecklistMaterial", TenantName, Material);
        }
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}/image")]
        public async Task<ActionResult<Material>> PostImageMaterial(string TenantName, ImageMaterial imageMaterial)
        {
            Material Material = new Material();
            Material.MaterialType = MaterialType.Image;
            Material.MaterialName = imageMaterial.MaterialName;
            Material.ParentId = imageMaterial.ParentId;
            Material.TrainingProgramList = imageMaterial.TrainingProgramList;
            Material.MaterialId = imageMaterial.MaterialId;
            if (imageMaterial.AssetId != null) {
                var Asset= await _context.Assets.FindAsync(imageMaterial.AssetId);
                if (Asset == null)
                {
                    return NotFound($"Couldnt Find Asset {imageMaterial.AssetId}");
                }
                
            }
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
            
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
             _context.Images.Add(imageMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostImageMaterial", TenantName, Material);
        }
        [HttpPost("/xr50/TrainingProgram_Asset_Repository/[controller]/{TenantName}/video")]
        public async Task<ActionResult<Material>> PostVideoMaterial(string TenantName, VideoMaterial videoMaterial)
        {
            Material Material = new Material();
            Material.MaterialType = MaterialType.Video;
            Material.MaterialName = videoMaterial.MaterialName;
            Material.ParentId = videoMaterial.ParentId;
            Material.TrainingProgramList = videoMaterial.TrainingProgramList;
            Material.MaterialId = videoMaterial.MaterialId;
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
                
            }
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
            
            Material.MaterialId = Guid.NewGuid().ToString();
            _context.Materials.Add(Material);
            _context.Videos.Add(videoMaterial);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction("PostVideoMaterial", TenantName, Material);
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

            foreach (string trainingId in Material.TrainingProgramList) {

	            var TrainingProgram = await _context.TrainingPrograms.FindAsync(Material.TenantName,trainingId);
                if (TrainingProgram == null)
                {
                    return NotFound();
                }
	            TrainingProgram.MaterialList.Remove(MaterialId);
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
        private bool MaterialManagementExists(string MaterialName)
        {
            return _context.Materials.Any(e => e.MaterialName.Equals(MaterialName));
        }
        
    }


}
