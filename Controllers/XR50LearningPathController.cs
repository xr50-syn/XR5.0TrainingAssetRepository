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
    public class LearningPathsController : ControllerBase
    {
        private readonly ILearningPathService _learningPathService;
        private readonly ILogger<LearningPathsController> _logger;

        public LearningPathsController(
            ILearningPathService learningPathService,
            ILogger<LearningPathsController> logger)
        {
            _learningPathService = learningPathService;
            _logger = logger;
        }

        // GET: api/{tenantName}/learningpaths
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LearningPath>>> GetLearningPaths(string tenantName)
        {
            _logger.LogInformation("Getting learning paths for tenant: {TenantName}", tenantName);
            
            var learningPaths = await _learningPathService.GetAllLearningPathsAsync();
            
            _logger.LogInformation("Found {LearningPathCount} learning paths for tenant: {TenantName}", 
                learningPaths.Count(), tenantName);
            
            return Ok(learningPaths);
        }

        // GET: api/{tenantName}/learningpaths/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LearningPath>> GetLearningPath(string tenantName, int id)
        {
            _logger.LogInformation("Getting learning path {Id} for tenant: {TenantName}", id, tenantName);
            
            var learningPath = await _learningPathService.GetLearningPathAsync(id);

            if (learningPath == null)
            {
                _logger.LogWarning("Learning path {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return learningPath;
        }

        // POST: api/{tenantName}/learningpaths
        [HttpPost]
        public async Task<ActionResult<LearningPath>> PostLearningPath(string tenantName, LearningPath learningPath)
        {
            _logger.LogInformation("Creating learning path {Name} for tenant: {TenantName}", 
                learningPath.LearningPathName, tenantName);
            
            var createdLearningPath = await _learningPathService.CreateLearningPathAsync(learningPath);

            _logger.LogInformation("Created learning path {Name} with ID {Id} for tenant: {TenantName}", 
                createdLearningPath.LearningPathName, createdLearningPath.Id, tenantName);

            return CreatedAtAction(nameof(GetLearningPath), 
                new { tenantName, id = createdLearningPath.Id }, 
                createdLearningPath);
        }

        // PUT: api/{tenantName}/learningpaths/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutLearningPath(string tenantName, int id, LearningPath learningPath)
        {
            if (id != learningPath.Id)
            {
                return BadRequest("ID mismatch");
            }

            _logger.LogInformation("Updating learning path {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _learningPathService.UpdateLearningPathAsync(learningPath);
                _logger.LogInformation("Updated learning path {Id} for tenant: {TenantName}", id, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _learningPathService.LearningPathExistsAsync(id))
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

        // DELETE: api/{tenantName}/learningpaths/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLearningPath(string tenantName, int id)
        {
            _logger.LogInformation("Deleting learning path {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _learningPathService.DeleteLearningPathAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("Deleted learning path {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }

        // GET: api/{tenantName}/learningpaths/program/{trainingProgramId}
        [HttpGet("program/{trainingProgramId}")]
        public async Task<ActionResult<IEnumerable<LearningPath>>> GetLearningPathsByTrainingProgram(string tenantName, int trainingProgramId)
        {
            _logger.LogInformation("Getting learning paths for training program {TrainingProgramId} in tenant: {TenantName}", 
                trainingProgramId, tenantName);
            
            var learningPaths = await _learningPathService.GetLearningPathsByTrainingProgramAsync(trainingProgramId);
            
            _logger.LogInformation("Found {Count} learning paths for training program {TrainingProgramId} in tenant: {TenantName}", 
                learningPaths.Count(), trainingProgramId, tenantName);
            
            return Ok(learningPaths);
        }

        // POST: api/{tenantName}/learningpaths/{learningPathId}/assign/{trainingProgramId}
        [HttpPost("{learningPathId}/assign/{trainingProgramId}")]
        public async Task<IActionResult> AssignLearningPathToTrainingProgram(string tenantName, int learningPathId, int trainingProgramId)
        {
            _logger.LogInformation("🔗 Assigning learning path {LearningPathId} to training program {TrainingProgramId} for tenant: {TenantName}", 
                learningPathId, trainingProgramId, tenantName);
            
            var success = await _learningPathService.AssignLearningPathToTrainingProgramAsync(trainingProgramId, learningPathId);
            
            if (!success)
            {
                return BadRequest("Association already exists or entities not found");
            }

            _logger.LogInformation("Successfully assigned learning path {LearningPathId} to training program {TrainingProgramId} for tenant: {TenantName}", 
                learningPathId, trainingProgramId, tenantName);

            return Ok(new { Message = "Learning path successfully assigned to training program" });
        }

        // DELETE: api/{tenantName}/learningpaths/{learningPathId}/unassign/{trainingProgramId}
        [HttpDelete("{learningPathId}/unassign/{trainingProgramId}")]
        public async Task<IActionResult> RemoveLearningPathFromTrainingProgram(string tenantName, int learningPathId, int trainingProgramId)
        {
            _logger.LogInformation("Removing learning path {LearningPathId} from training program {TrainingProgramId} for tenant: {TenantName}", 
                learningPathId, trainingProgramId, tenantName);
            
            var success = await _learningPathService.RemoveLearningPathFromTrainingProgramAsync(trainingProgramId, learningPathId);
            
            if (!success)
            {
                return NotFound("Association not found");
            }

            _logger.LogInformation("Successfully removed learning path {LearningPathId} from training program {TrainingProgramId} for tenant: {TenantName}", 
                learningPathId, trainingProgramId, tenantName);

            return Ok(new { Message = "Learning path successfully removed from training program" });
        }
    }
}