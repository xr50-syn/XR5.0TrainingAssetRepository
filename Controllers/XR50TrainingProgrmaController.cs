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
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<TrainingProgramsController> _logger;

        public TrainingProgramsController(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<TrainingProgramsController> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // GET: api/{tenantName}/trainingPrograms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrainingProgram>>> GetTrainingPrograms(string tenantName)
        {
            _logger.LogInformation("Getting trainingPrograms for tenant: {TenantName}", tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var trainingPrograms = await context.TrainingPrograms.ToListAsync();
            
            _logger.LogInformation("Found {TrainingProgramCount} trainingPrograms for tenant: {TenantName}", trainingPrograms.Count, tenantName);
            
            return trainingPrograms;
        }

        // GET: api/{tenantName}/trainingPrograms/5
        [HttpGet("{trainingProgramName}")]
        public async Task<ActionResult<TrainingProgram>> GetTrainingProgram(string tenantName, string trainingProgramName)
        {
            _logger.LogInformation("Getting trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgramName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var trainingProgram = await context.TrainingPrograms.FindAsync(trainingProgramName);

            if (trainingProgram == null)
            {
                _logger.LogWarning("TrainingProgram {TrainingProgramName} not found in tenant: {TenantName}", trainingProgramName, tenantName);
                return NotFound();
            }

            return trainingProgram;
        }

        // POST: api/{tenantName}/trainingPrograms
        [HttpPost]
        public async Task<ActionResult<TrainingProgram>> PostTrainingProgram(string tenantName, TrainingProgram trainingProgram)
        {
            _logger.LogInformation(" Creating trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgram.Name, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.TrainingPrograms.Add(trainingProgram);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgram.Name, tenantName);

            return CreatedAtAction(nameof(GetTrainingProgram), 
                new { tenantName, trainingProgramName = trainingProgram.Name }, 
                trainingProgram);
        }

        // PUT: api/{tenantName}/trainingPrograms/5
        [HttpPut("{trainingProgramName}")]
        public async Task<IActionResult> PutTrainingProgram(string tenantName, string trainingProgramName, TrainingProgram trainingProgram)
        {
            if (trainingProgramName != trainingProgram.Name)
            {
                return BadRequest();
            }

            _logger.LogInformation(" Updating trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgramName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(trainingProgram).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgramName, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TrainingProgramExistsAsync(trainingProgramName))
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

        // DELETE: api/{tenantName}/trainingPrograms/5
        [HttpDelete("{trainingProgramName}")]
        public async Task<IActionResult> DeleteTrainingProgram(string tenantName, string trainingProgramName)
        {
            _logger.LogInformation("Deleting trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgramName, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var trainingProgram = await context.TrainingPrograms.FindAsync(trainingProgramName);
            if (trainingProgram == null)
            {
                return NotFound();
            }

            context.TrainingPrograms.Remove(trainingProgram);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted trainingProgram {TrainingProgramName} for tenant: {TenantName}", trainingProgramName, tenantName);

            return NoContent();
        }

        private async Task<bool> TrainingProgramExistsAsync(string trainingProgramName)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms.AnyAsync(e => e.Name == trainingProgramName);
        }
    }
}