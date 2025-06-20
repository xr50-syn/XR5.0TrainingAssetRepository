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
        private readonly ILogger<TrainingProgramsController> _logger;

        public TrainingProgramsController(
            ITrainingProgramService trainingProgramService,
            ILogger<TrainingProgramsController> logger)
        {
            _trainingProgramService = trainingProgramService;
            _logger = logger;
        }

        // GET: api/{tenantName}/trainingprograms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetTrainingPrograms(string tenantName)
        {
            _logger.LogInformation("🔍 Getting training programs for tenant: {TenantName}", tenantName);
            
            var programs = await _trainingProgramService.GetAllTrainingProgramsAsync();
            
            _logger.LogInformation("✅ Found {ProgramCount} training programs for tenant: {TenantName}", programs.Count(), tenantName);
            
            return Ok(programs);
        }

        // GET: api/{tenantName}/trainingprograms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TrainingProgram>> GetTrainingProgram(string tenantName, int id)
        {
            _logger.LogInformation("🔍 Getting training program {Id} for tenant: {TenantName}", id, tenantName);
            
            var program = await _trainingProgramService.GetTrainingProgramAsync(id);

            if (program == null)
            {
                _logger.LogWarning("❌ Training program {Id} not found in tenant: {TenantName}", id, tenantName);
                return NotFound();
            }

            return program;
        }

        // POST: api/{tenantName}/trainingprograms
        [HttpPost]
        public async Task<ActionResult<TrainingProgram>> PostTrainingProgram(string tenantName, TrainingProgram program)
        {
            _logger.LogInformation("📝 Creating training program {Name} for tenant: {TenantName}", program.Name, tenantName);
            
            var createdProgram = await _trainingProgramService.CreateTrainingProgramAsync(program);

            _logger.LogInformation("✅ Created training program {Name} with ID {Id} for tenant: {TenantName}", 
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

            _logger.LogInformation("📝 Updating training program {Id} for tenant: {TenantName}", id, tenantName);
            
            try
            {
                await _trainingProgramService.UpdateTrainingProgramAsync(program);
                _logger.LogInformation("✅ Updated training program {Id} for tenant: {TenantName}", id, tenantName);
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
            _logger.LogInformation("🗑️ Deleting training program {Id} for tenant: {TenantName}", id, tenantName);
            
            var deleted = await _trainingProgramService.DeleteTrainingProgramAsync(id);
            
            if (!deleted)
            {
                return NotFound();
            }

            _logger.LogInformation("✅ Deleted training program {Id} for tenant: {TenantName}", id, tenantName);

            return NoContent();
        }
    }
}