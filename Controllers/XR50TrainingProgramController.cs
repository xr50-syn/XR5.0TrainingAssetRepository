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
    public class TrainingProgramsController : ControllerBase
    {
        private readonly ITrainingProgramService _trainingProgramService;
        private readonly IMaterialService _materialService;
        private readonly ILogger<TrainingProgramsController> _logger;

        public TrainingProgramsController(
            ITrainingProgramService trainingProgramService,
            IMaterialService materialService,
            ILogger<TrainingProgramsController> logger)
        {
            _trainingProgramService = trainingProgramService;
            _materialService = materialService;
            _logger = logger;
        }

        // GET: api/{tenantName}/trainingprograms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetTrainingPrograms(string tenantName)
        {
            _logger.LogInformation("Getting training programs for tenant: {TenantName}", tenantName);
            
            var programs = await _trainingProgramService.GetAllTrainingProgramsAsync();
            
            _logger.LogInformation("Found {ProgramCount} training programs for tenant: {TenantName}", programs.Count(), tenantName);
            
            return Ok(programs);
        }

        // GET: api/{tenantName}/trainingprograms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrainingProgram>> GetTrainingProgram(string tenantName, int id)
        {
            _logger.LogInformation("Getting training program {Id} for tenant: {TenantName}", id, tenantName);
            
            var program = await _trainingProgramService.GetTrainingProgramAsync(id);

            if (program == null)
            {
                _logger.LogWarning("Training program {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return program;
        }

        // POST: api/{tenantName}/trainingprograms
        [HttpPost]
        public async Task<ActionResult<TrainingProgram>> PostTrainingProgram(string tenantName, TrainingProgram program)
        {
            _logger.LogInformation("Creating training program {Name} for tenant: {TenantName}", program.Name, tenantName);
            
            var createdProgram = await _trainingProgramService.CreateTrainingProgramAsync(program);

            _logger.LogInformation("Created training program {Name} with ID {Id} for tenant: {TenantName}", 
                createdProgram.Name, createdProgram.Id, tenantName);

            return CreatedAtAction(nameof(GetTrainingProgram), 
                new { tenantName, id = createdProgram.Id }, 
                createdProgram);
        }

        // PUT: api/{tenantName}/trainingprograms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrainingProgram(string tenantName, int id, TrainingProgram program)
        {
            if (id != program.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating training program {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _trainingProgramService.UpdateTrainingProgramAsync(program);
                _logger.LogInformation("Updated training program {Id} for tenant: {TenantName}", id, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _trainingProgramService.TrainingProgramExistsAsync(id))
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

        // DELETE: api/{tenantName}/trainingprograms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrainingProgram(string tenantName, int id)
        {
            _logger.LogInformation("Deleting training program {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _trainingProgramService.DeleteTrainingProgramAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Deleted training program {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }
   #region Material Assignment Endpoints

        /// <summary>
        /// Get all materials assigned to this training program
        /// </summary>
        [HttpGet("{trainingProgramId}/materials")]
        public async Task<ActionResult<IEnumerable<Material>>> GetTrainingProgramMaterials(
            string tenantName,
            int trainingProgramId,
            [FromQuery] bool includeOrder = true)
        {
            _logger.LogInformation("📚 Getting materials for training program {TrainingProgramId} for tenant: {TenantName}",
                trainingProgramId, tenantName);

            // Verify training program exists
            var program = await _trainingProgramService.GetTrainingProgramAsync(trainingProgramId);
            if (program == null)
            {
                return NotFound($"Training program {trainingProgramId} not found");
            }

            var materials = await _materialService.GetMaterialsByTrainingProgramAsync(trainingProgramId);

            _logger.LogInformation("Found {Count} materials for training program {TrainingProgramId} for tenant: {TenantName}",
                materials.Count(), trainingProgramId, tenantName);

            return Ok(materials);
        }

        /// <summary>
        /// Assign a material to this training program
        /// </summary>
        [HttpPost("{trainingProgramId}/assign-material/{materialId}")]
        public async Task<ActionResult<object>> AssignMaterialToTrainingProgram(
            string tenantName,
            int trainingProgramId,
            int materialId,
            [FromQuery] string relationshipType = "assigned",
            [FromQuery] int? displayOrder = null)
        {
            _logger.LogInformation("🔗 Assigning material {MaterialId} to training program {TrainingProgramId} for tenant: {TenantName}",
                materialId, trainingProgramId, tenantName);

            try
            {
                var relationshipId = await _materialService.AssignMaterialToTrainingProgramAsync(
                    materialId, trainingProgramId, relationshipType);

                _logger.LogInformation("Successfully assigned material {MaterialId} to training program {TrainingProgramId} (Relationship: {RelationshipId}) for tenant: {TenantName}",
                    materialId, trainingProgramId, relationshipId, tenantName);

                return Ok(new
                {
                    Message = "Material successfully assigned to training program",
                    RelationshipId = relationshipId,
                    TrainingProgramId = trainingProgramId,
                    MaterialId = materialId,
                    RelationshipType = relationshipType
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Remove a material from this training program
        /// </summary>
        [HttpDelete("{trainingProgramId}/remove-material/{materialId}")]
        public async Task<IActionResult> RemoveMaterialFromTrainingProgram(
            string tenantName,
            int trainingProgramId,
            int materialId)
        {
            _logger.LogInformation("Removing material {MaterialId} from training program {TrainingProgramId} for tenant: {TenantName}",
                materialId, trainingProgramId, tenantName);

            var success = await _materialService.RemoveMaterialFromTrainingProgramAsync(materialId, trainingProgramId);

            if (!success)
            {
                return NotFound("Material assignment not found");
            }

            _logger.LogInformation("Successfully removed material {MaterialId} from training program {TrainingProgramId} for tenant: {TenantName}",
                materialId, trainingProgramId, tenantName);

            return Ok(new { Message = "Material successfully removed from training program" });
        }

        /// <summary>
        /// Bulk assign materials to this training program
        /// </summary>
        /*[HttpPost("{trainingProgramId}/bulk-assign-materials")]
        public async Task<ActionResult<BulkAssignmentResult>> BulkAssignMaterialsToTrainingProgram(
            string tenantName,
            int trainingProgramId,
            [FromBody] IEnumerable<BulkMaterialAssignment> assignments)
        {
            _logger.LogInformation("⚡ Bulk assigning {Count} materials to training program {TrainingProgramId} for tenant: {TenantName}",
                assignments.Count(), trainingProgramId, tenantName);

            var result = new BulkAssignmentResult();

            foreach (var assignment in assignments)
            {
                try
                {
                    var relationshipId = await _materialService.AssignMaterialToTrainingProgramAsync(
                        assignment.MaterialId,
                        trainingProgramId,
                        assignment.RelationshipType ?? "assigned");

                    result.SuccessfulAssignments++;
                }
                catch (Exception ex)
                {
                    result.FailedAssignments++;
                    result.Errors.Add($"Error assigning material {assignment.MaterialId}: {ex.Message}");
                }
            }

            _logger.LogInformation("Bulk assignment complete: {Success} successful, {Failed} failed for training program {TrainingProgramId} for tenant: {TenantName}",
                result.SuccessfulAssignments, result.FailedAssignments, trainingProgramId, tenantName);

            return Ok(result);
        }*/

        #endregion
    }
}