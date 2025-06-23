using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Data;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Controllers
{
    [Route("api/{tenantName}/[controller]")]
    [ApiController]
    public class MaterialsController : ControllerBase
    {
        private readonly IMaterialService _materialService;
        private readonly ILogger<MaterialsController> _logger;

        public MaterialsController(
            IMaterialService materialService,
            ILogger<MaterialsController> logger)
        {
            _materialService = materialService;
            _logger = logger;
        }

        #region Base Material Operations

        // GET: api/{tenantName}/materials
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterials(string tenantName)
        {
            _logger.LogInformation("Getting materials for tenant: {TenantName}", tenantName);
            
            var materials = await _materialService.GetAllMaterialsAsync();
            
            _logger.LogInformation("Found {MaterialCount} materials for tenant: {TenantName}", 
                materials.Count(), tenantName);
            
            return Ok(materials);
        }

        // GET: api/{tenantName}/materials/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Material>> GetMaterial(string tenantName, int id)
        {
            _logger.LogInformation("Getting material {Id} for tenant: {TenantName}", id, tenantName);
            
            var material = await _materialService.GetMaterialAsync(id);

            if (material == null)
            {
                _logger.LogWarning("Material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return material;
        }

        // POST: api/{tenantName}/materials
        [HttpPost]
        public async Task<ActionResult<Material>> PostMaterial(string tenantName, Material material)
        {
            _logger.LogInformation("Creating material {Name} (Type: {Type}) for tenant: {TenantName}", 
                material.Name, material.GetType().Name, tenantName);
            
            var createdMaterial = await _materialService.CreateMaterialAsync(material);

            _logger.LogInformation("Created material {Name} with ID {Id} for tenant: {TenantName}", 
                createdMaterial.Name, createdMaterial.Id, tenantName);

            return CreatedAtAction(nameof(GetMaterial), 
                new { tenantName, id = createdMaterial.Id }, 
                createdMaterial);
        }

        // PUT: api/{tenantName}/materials/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutMaterial(string tenantName, int id, Material material)
        {
            if (id != material.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating material {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _materialService.UpdateMaterialAsync(material);
                _logger.LogInformation("Updated material {Id} for tenant: {TenantName}", id, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _materialService.MaterialExistsAsync(id))
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

        // DELETE: api/{tenantName}/materials/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(string tenantName, int id)
        {
            _logger.LogInformation("Deleting material {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _materialService.DeleteMaterialAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Deleted material {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }

        #endregion

        #region Material Type-Specific Endpoints

        // GET: api/{tenantName}/materials/videos
        [HttpGet("videos")]
        public async Task<ActionResult<IEnumerable<VideoMaterial>>> GetVideoMaterials(string tenantName)
        {
            _logger.LogInformation("🎥 Getting video materials for tenant: {TenantName}", tenantName);
            
            var videos = await _materialService.GetAllVideoMaterialsAsync();
            
            _logger.LogInformation("Found {Count} video materials for tenant: {TenantName}", 
                videos.Count(), tenantName);
            
            return Ok(videos);
        }

        // GET: api/{tenantName}/materials/checklists
        [HttpGet("checklists")]
        public async Task<ActionResult<IEnumerable<ChecklistMaterial>>> GetChecklistMaterials(string tenantName)
        {
            _logger.LogInformation("📋 Getting checklist materials for tenant: {TenantName}", tenantName);
            
            var checklists = await _materialService.GetAllChecklistMaterialsAsync();
            
            _logger.LogInformation("Found {Count} checklist materials for tenant: {TenantName}", 
                checklists.Count(), tenantName);
            
            return Ok(checklists);
        }

        // GET: api/{tenantName}/materials/workflows
        [HttpGet("workflows")]
        public async Task<ActionResult<IEnumerable<WorkflowMaterial>>> GetWorkflowMaterials(string tenantName)
        {
            _logger.LogInformation("⚙️ Getting workflow materials for tenant: {TenantName}", tenantName);
            
            var workflows = await _materialService.GetAllWorkflowMaterialsAsync();
            
            _logger.LogInformation("Found {Count} workflow materials for tenant: {TenantName}", 
                workflows.Count(), tenantName);
            
            return Ok(workflows);
        }

        // GET: api/{tenantName}/materials/images
        [HttpGet("images")]
        public async Task<ActionResult<IEnumerable<ImageMaterial>>> GetImageMaterials(string tenantName)
        {
            _logger.LogInformation("🖼️ Getting image materials for tenant: {TenantName}", tenantName);
            
            var images = await _materialService.GetAllImageMaterialsAsync();
            
            _logger.LogInformation("Found {Count} image materials for tenant: {TenantName}", 
                images.Count(), tenantName);
            
            return Ok(images);
        }

        #endregion

        #region Video Material Management

        // GET: api/{tenantName}/materials/videos/5/with-timestamps
        [HttpGet("videos/{id}/with-timestamps")]
        public async Task<ActionResult<VideoMaterial>> GetVideoWithTimestamps(string tenantName, int id)
        {
            _logger.LogInformation("🎥 Getting video material {Id} with timestamps for tenant: {TenantName}", 
                id, tenantName);
            
            var video = await _materialService.GetVideoMaterialWithTimestampsAsync(id);
            
            if (video == null)
            {
                _logger.LogWarning("Video material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved video material {Id} with {Count} timestamps for tenant: {TenantName}", 
                id, video.VideoTimestamps?.Count() ?? 0, tenantName);
            
            return Ok(video);
        }

        // POST: api/{tenantName}/materials/videos/5/timestamps
        [HttpPost("videos/{videoId}/timestamps")]
        public async Task<ActionResult<VideoMaterial>> AddTimestampToVideo(string tenantName, int videoId, VideoTimestamp timestamp)
        {
            _logger.LogInformation("⏱️ Adding timestamp '{Title}' to video {VideoId} for tenant: {TenantName}", 
                timestamp.Title, videoId, tenantName);
            
            try
            {
                var video = await _materialService.AddTimestampToVideoAsync(videoId, timestamp);
                
                _logger.LogInformation("Added timestamp to video {VideoId} for tenant: {TenantName}", 
                    videoId, tenantName);
                
                return Ok(video);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/{tenantName}/materials/videos/5/timestamps/3
        [HttpDelete("videos/{videoId}/timestamps/{timestampId}")]
        public async Task<IActionResult> RemoveTimestampFromVideo(string tenantName, int videoId, int timestampId)
        {
            _logger.LogInformation("Removing timestamp {TimestampId} from video {VideoId} for tenant: {TenantName}", 
                timestampId, videoId, tenantName);
            
            var removed = await _materialService.RemoveTimestampFromVideoAsync(videoId, timestampId);
            
            if (!removed)
            {
                return NotFound("Timestamp not found");
            }

            _logger.LogInformation("Removed timestamp {TimestampId} from video {VideoId} for tenant: {TenantName}", 
                timestampId, videoId, tenantName);

            return NoContent();
        }

        #endregion

        #region Checklist Material Management

        // GET: api/{tenantName}/materials/checklists/5/with-entries
        [HttpGet("checklists/{id}/with-entries")]
        public async Task<ActionResult<ChecklistMaterial>> GetChecklistWithEntries(string tenantName, int id)
        {
            _logger.LogInformation("📋 Getting checklist material {Id} with entries for tenant: {TenantName}", 
                id, tenantName);
            
            var checklist = await _materialService.GetChecklistMaterialWithEntriesAsync(id);
            
            if (checklist == null)
            {
                _logger.LogWarning("Checklist material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved checklist material {Id} with {Count} entries for tenant: {TenantName}", 
                id, checklist.ChecklistEntries?.Count() ?? 0, tenantName);
            
            return Ok(checklist);
        }

        // POST: api/{tenantName}/materials/checklists/5/entries
        [HttpPost("checklists/{checklistId}/entries")]
        public async Task<ActionResult<ChecklistMaterial>> AddEntryToChecklist(string tenantName, int checklistId, ChecklistEntry entry)
        {
            _logger.LogInformation("✔️ Adding entry '{Text}' to checklist {ChecklistId} for tenant: {TenantName}", 
                entry.Text, checklistId, tenantName);
            
            try
            {
                var checklist = await _materialService.AddEntryToChecklistAsync(checklistId, entry);
                
                _logger.LogInformation("Added entry to checklist {ChecklistId} for tenant: {TenantName}", 
                    checklistId, tenantName);
                
                return Ok(checklist);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/{tenantName}/materials/checklists/5/entries/3
        [HttpDelete("checklists/{checklistId}/entries/{entryId}")]
        public async Task<IActionResult> RemoveEntryFromChecklist(string tenantName, int checklistId, int entryId)
        {
            _logger.LogInformation("Removing entry {EntryId} from checklist {ChecklistId} for tenant: {TenantName}", 
                entryId, checklistId, tenantName);
            
            var removed = await _materialService.RemoveEntryFromChecklistAsync(checklistId, entryId);
            
            if (!removed)
            {
                return NotFound("Entry not found");
            }

            _logger.LogInformation("Removed entry {EntryId} from checklist {ChecklistId} for tenant: {TenantName}", 
                entryId, checklistId, tenantName);

            return NoContent();
        }

        #endregion

        #region Workflow Material Management

        // GET: api/{tenantName}/materials/workflows/5/with-steps
        [HttpGet("workflows/{id}/with-steps")]
        public async Task<ActionResult<WorkflowMaterial>> GetWorkflowWithSteps(string tenantName, int id)
        {
            _logger.LogInformation("⚙️ Getting workflow material {Id} with steps for tenant: {TenantName}", 
                id, tenantName);
            
            var workflow = await _materialService.GetWorkflowMaterialWithStepsAsync(id);
            
            if (workflow == null)
            {
                _logger.LogWarning("Workflow material {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            _logger.LogInformation("Retrieved workflow material {Id} with {Count} steps for tenant: {TenantName}", 
                id, workflow.WorkflowSteps?.Count() ?? 0, tenantName);
            
            return Ok(workflow);
        }

        // POST: api/{tenantName}/materials/workflows/5/steps
        [HttpPost("workflows/{workflowId}/steps")]
        public async Task<ActionResult<WorkflowMaterial>> AddStepToWorkflow(string tenantName, int workflowId, WorkflowStep step)
        {
            _logger.LogInformation("➕ Adding step '{Title}' to workflow {WorkflowId} for tenant: {TenantName}", 
                step.Title, workflowId, tenantName);
            
            try
            {
                var workflow = await _materialService.AddStepToWorkflowAsync(workflowId, step);
                
                _logger.LogInformation("Added step to workflow {WorkflowId} for tenant: {TenantName}", 
                    workflowId, tenantName);
                
                return Ok(workflow);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("{Message} for tenant: {TenantName}", ex.Message, tenantName);
                return NotFound(ex.Message);
            }
        }

        // DELETE: api/{tenantName}/materials/workflows/5/steps/3
        [HttpDelete("workflows/{workflowId}/steps/{stepId}")]
        public async Task<IActionResult> RemoveStepFromWorkflow(string tenantName, int workflowId, int stepId)
        {
            _logger.LogInformation("Removing step {StepId} from workflow {WorkflowId} for tenant: {TenantName}", 
                stepId, workflowId, tenantName);
            
            var removed = await _materialService.RemoveStepFromWorkflowAsync(workflowId, stepId);
            
            if (!removed)
            {
                return NotFound("Step not found");
            }

            _logger.LogInformation("Removed step {StepId} from workflow {WorkflowId} for tenant: {TenantName}", 
                stepId, workflowId, tenantName);

            return NoContent();
        }

        #endregion

        #region Asset Relationships (Only for Video, Image, Unity, Default Materials)

        // GET: api/{tenantName}/materials/by-asset/asset123
        [HttpGet("by-asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialsByAsset(string tenantName, string assetId)
        {
            _logger.LogInformation("Getting materials for asset {AssetId} in tenant: {TenantName}", 
                assetId, tenantName);
            
            var materials = await _materialService.GetMaterialsByAssetIdAsync(assetId);
            
            _logger.LogInformation("Found {Count} materials for asset {AssetId} in tenant: {TenantName}", 
                materials.Count(), assetId, tenantName);
            
            return Ok(materials);
        }

        // GET: api/{tenantName}/materials/5/asset
        [HttpGet("{materialId}/asset")]
        public async Task<ActionResult<object>> GetMaterialAsset(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting asset for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var assetId = await _materialService.GetMaterialAssetIdAsync(materialId);
            
            if (assetId == null)
            {
                return Ok(new { AssetId = (string?)null, Message = "Material does not support assets or has no asset assigned" });
            }

            _logger.LogInformation("Material {MaterialId} has asset {AssetId} in tenant: {TenantName}", 
                materialId, assetId, tenantName);
            
            return Ok(new { AssetId = assetId });
        }

        // POST: api/{tenantName}/materials/5/assign-asset/asset123
        [HttpPost("{materialId}/assign-asset/{assetId}")]
        public async Task<IActionResult> AssignAssetToMaterial(string tenantName, int materialId, string assetId)
        {
            _logger.LogInformation("🔗 Assigning asset {AssetId} to material {MaterialId} for tenant: {TenantName}", 
                assetId, materialId, tenantName);
            
            var success = await _materialService.AssignAssetToMaterialAsync(materialId, assetId);
            
            if (!success)
            {
                return BadRequest("Material not found or material type does not support assets");
            }

            _logger.LogInformation("Assigned asset {AssetId} to material {MaterialId} for tenant: {TenantName}", 
                assetId, materialId, tenantName);

            return Ok(new { Message = "Asset successfully assigned to material" });
        }

        // DELETE: api/{tenantName}/materials/5/remove-asset
        [HttpDelete("{materialId}/remove-asset")]
        public async Task<IActionResult> RemoveAssetFromMaterial(string tenantName, int materialId)
        {
            _logger.LogInformation("Removing asset from material {MaterialId} for tenant: {TenantName}", 
                materialId, tenantName);
            
            var success = await _materialService.RemoveAssetFromMaterialAsync(materialId);
            
            if (!success)
            {
                return BadRequest("Material not found or material type does not support assets");
            }

            _logger.LogInformation("Removed asset from material {MaterialId} for tenant: {TenantName}", 
                materialId, tenantName);

            return Ok(new { Message = "Asset successfully removed from material" });
        }

        #endregion

        #region Polymorphic Relationships Management

        // GET: api/{tenantName}/materials/5/relationships
        [HttpGet("{materialId}/relationships")]
        public async Task<ActionResult<IEnumerable<MaterialRelationship>>> GetMaterialRelationships(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting relationships for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var relationships = await _materialService.GetMaterialRelationshipsAsync(materialId);
            
            _logger.LogInformation("Found {Count} relationships for material {MaterialId} in tenant: {TenantName}", 
                relationships.Count(), materialId, tenantName);
            
            return Ok(relationships);
        }

        // GET: api/{tenantName}/materials/5/relationships/LearningPath
        [HttpGet("{materialId}/relationships/{entityType}")]
        public async Task<ActionResult<IEnumerable<MaterialRelationship>>> GetMaterialRelationshipsByType(
            string tenantName, int materialId, string entityType)
        {
            _logger.LogInformation("Getting {EntityType} relationships for material {MaterialId} in tenant: {TenantName}", 
                entityType, materialId, tenantName);
            
            var relationships = await _materialService.GetRelationshipsByTypeAsync(materialId, entityType);
            
            _logger.LogInformation("Found {Count} {EntityType} relationships for material {MaterialId} in tenant: {TenantName}", 
                relationships.Count(), entityType, materialId, tenantName);
            
            return Ok(relationships);
        }

        // POST: api/{tenantName}/materials/5/assign-learningpath/3
        [HttpPost("{materialId}/assign-learningpath/{learningPathId}")]
        public async Task<ActionResult<object>> AssignMaterialToLearningPath(
            string tenantName, int materialId, int learningPathId, [FromQuery] string relationshipType = "contains", [FromQuery] int? displayOrder = null)
        {
            _logger.LogInformation("🔗 Assigning material {MaterialId} to learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);
            
            var relationshipId = await _materialService.AssignMaterialToLearningPathAsync(materialId, learningPathId, relationshipType, displayOrder);

            _logger.LogInformation("Assigned material {MaterialId} to learning path {LearningPathId} (Relationship: {RelationshipId}) for tenant: {TenantName}", 
                materialId, learningPathId, relationshipId, tenantName);

            return Ok(new { 
                Message = "Material successfully assigned to learning path",
                RelationshipId = relationshipId,
                RelationshipType = relationshipType,
                DisplayOrder = displayOrder
            });
        }

        // DELETE: api/{tenantName}/materials/5/remove-learningpath/3
        [HttpDelete("{materialId}/remove-learningpath/{learningPathId}")]
        public async Task<IActionResult> RemoveMaterialFromLearningPath(string tenantName, int materialId, int learningPathId)
        {
            _logger.LogInformation("Removing material {MaterialId} from learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);
            
            var success = await _materialService.RemoveMaterialFromLearningPathAsync(materialId, learningPathId);
            
            if (!success)
            {
                return NotFound("Relationship not found");
            }

            _logger.LogInformation("Removed material {MaterialId} from learning path {LearningPathId} for tenant: {TenantName}", 
                materialId, learningPathId, tenantName);

            return Ok(new { Message = "Material successfully removed from learning path" });
        }

        // POST: api/{tenantName}/materials/5/assign-trainingprogram/2
        [HttpPost("{materialId}/assign-trainingprogram/{trainingProgramId}")]
        public async Task<ActionResult<object>> AssignMaterialToTrainingProgram(
            string tenantName, int materialId, int trainingProgramId, [FromQuery] string relationshipType = "assigned")
        {
            _logger.LogInformation("🔗 Assigning material {MaterialId} to training program {TrainingProgramId} for tenant: {TenantName}", 
                materialId, trainingProgramId, tenantName);
            
            var relationshipId = await _materialService.AssignMaterialToTrainingProgramAsync(materialId, trainingProgramId, relationshipType);

            _logger.LogInformation("Assigned material {MaterialId} to training program {TrainingProgramId} (Relationship: {RelationshipId}) for tenant: {TenantName}", 
                materialId, trainingProgramId, relationshipId, tenantName);

            return Ok(new { 
                Message = "Material successfully assigned to training program",
                RelationshipId = relationshipId,
                RelationshipType = relationshipType
            });
        }

        // DELETE: api/{tenantName}/materials/5/remove-trainingprogram/2
        [HttpDelete("{materialId}/remove-trainingprogram/{trainingProgramId}")]
        public async Task<IActionResult> RemoveMaterialFromTrainingProgram(string tenantName, int materialId, int trainingProgramId)
        {
            _logger.LogInformation("Removing material {MaterialId} from training program {TrainingProgramId} for tenant: {TenantName}", 
                materialId, trainingProgramId, tenantName);
            
            var success = await _materialService.RemoveMaterialFromTrainingProgramAsync(materialId, trainingProgramId);
            
            if (!success)
            {
                return NotFound("Relationship not found");
            }

            _logger.LogInformation("Removed material {MaterialId} from training program {TrainingProgramId} for tenant: {TenantName}", 
                materialId, trainingProgramId, tenantName);

            return Ok(new { Message = "Material successfully removed from training program" });
        }

        // POST: api/{tenantName}/materials/5/add-prerequisite/3
        [HttpPost("{materialId}/add-prerequisite/{prerequisiteMaterialId}")]
        public async Task<ActionResult<object>> AddMaterialPrerequisite(
            string tenantName, int materialId, int prerequisiteMaterialId, [FromQuery] string relationshipType = "prerequisite")
        {
            _logger.LogInformation("🔗 Adding prerequisite material {PrerequisiteId} to material {MaterialId} for tenant: {TenantName}", 
                prerequisiteMaterialId, materialId, tenantName);
            
            var relationshipId = await _materialService.CreateMaterialDependencyAsync(materialId, prerequisiteMaterialId, relationshipType);

            _logger.LogInformation("Added prerequisite material {PrerequisiteId} to material {MaterialId} (Relationship: {RelationshipId}) for tenant: {TenantName}", 
                prerequisiteMaterialId, materialId, relationshipId, tenantName);

            return Ok(new { 
                Message = "Material prerequisite successfully added",
                RelationshipId = relationshipId,
                RelationshipType = relationshipType
            });
        }

        // DELETE: api/{tenantName}/materials/5/remove-prerequisite/3
        [HttpDelete("{materialId}/remove-prerequisite/{prerequisiteMaterialId}")]
        public async Task<IActionResult> RemoveMaterialPrerequisite(string tenantName, int materialId, int prerequisiteMaterialId)
        {
            _logger.LogInformation("Removing prerequisite material {PrerequisiteId} from material {MaterialId} for tenant: {TenantName}", 
                prerequisiteMaterialId, materialId, tenantName);
            
            var success = await _materialService.RemoveMaterialDependencyAsync(materialId, prerequisiteMaterialId);
            
            if (!success)
            {
                return NotFound("Prerequisite relationship not found");
            }

            _logger.LogInformation("Removed prerequisite material {PrerequisiteId} from material {MaterialId} for tenant: {TenantName}", 
                prerequisiteMaterialId, materialId, tenantName);

            return Ok(new { Message = "Material prerequisite successfully removed" });
        }

        // GET: api/{tenantName}/materials/5/prerequisites
        [HttpGet("{materialId}/prerequisites")]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialPrerequisites(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting prerequisites for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var prerequisites = await _materialService.GetMaterialPrerequisitesAsync(materialId);
            
            _logger.LogInformation("Found {Count} prerequisites for material {MaterialId} in tenant: {TenantName}", 
                prerequisites.Count(), materialId, tenantName);
            
            return Ok(prerequisites);
        }

        // GET: api/{tenantName}/materials/5/dependents
        [HttpGet("{materialId}/dependents")]
        public async Task<ActionResult<IEnumerable<Material>>> GetMaterialDependents(string tenantName, int materialId)
        {
            _logger.LogInformation("Getting dependent materials for material {MaterialId} in tenant: {TenantName}", 
                materialId, tenantName);
            
            var dependents = await _materialService.GetMaterialDependentsAsync(materialId);
            
            _logger.LogInformation("Found {Count} dependent materials for material {MaterialId} in tenant: {TenantName}", 
                dependents.Count(), materialId, tenantName);
            
            return Ok(dependents);
        }

        #endregion
    }
}