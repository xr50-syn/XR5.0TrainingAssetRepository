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
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<LearningPathsController> _logger;

        public LearningPathsController(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<LearningPathsController> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // GET: api/{tenantName}/learningPaths
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LearningPath>>> GetLearningPaths(string tenantName)
        {
            _logger.LogInformation("Getting learningPaths for tenant: {TenantName}", tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var learningPaths = await context.LearningPaths.ToListAsync();
            
            _logger.LogInformation("Found {LearningPathCount} learningPaths for tenant: {TenantName}", learningPaths.Count, tenantName);
            
            return learningPaths;
        }

        // GET: api/{tenantName}/learningPaths/5
        [HttpGet("{learningPathId}")]
        public async Task<ActionResult<LearningPath>> GetLearningPath(string tenantName, int learningPathId)
        {
            _logger.LogInformation("Getting learningPath {LearningPathId} for tenant: {TenantName}", learningPathId, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var learningPath = await context.LearningPaths.FindAsync(learningPathId);

            if (learningPath == null)
            {
                _logger.LogWarning("LearningPath {LearningPathId} not found in tenant: {TenantName}", learningPathId, tenantName);
                return NotFound();
            }

            return learningPath;
        }

        // POST: api/{tenantName}/learningPaths
        [HttpPost]
        public async Task<ActionResult<LearningPath>> PostLearningPath(string tenantName, LearningPath learningPath)
        {
            _logger.LogInformation(" Creating learningPath {LearningPathId} for tenant: {TenantName}", learningPath.Id, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.LearningPaths.Add(learningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created learningPath {LearningPathId} for tenant: {TenantName}", learningPath.Id, tenantName);

            return CreatedAtAction(nameof(GetLearningPath), 
                new { tenantName, learningPathId = learningPath.Id }, 
                learningPath);
        }

        // PUT: api/{tenantName}/learningPaths/5
        [HttpPut("{learningPathId}")]
        public async Task<IActionResult> PutLearningPath(string tenantName, int learningPathId, LearningPath learningPath)
        {
            if (learningPathId != learningPath.Id)
            {
                return BadRequest();
            }

            _logger.LogInformation(" Updating learningPath {LearningPathId} for tenant: {TenantName}", learningPathId, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(learningPath).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Updated learningPath {LearningPathId} for tenant: {TenantName}", learningPathId, tenantName);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await LearningPathExistsAsync(learningPathId))
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

        // DELETE: api/{tenantName}/learningPaths/5
        [HttpDelete("{learningPathId}")]
        public async Task<IActionResult> DeleteLearningPath(string tenantName, string learningPathId)
        {
            _logger.LogInformation("Deleting learningPath {LearningPathId} for tenant: {TenantName}", learningPathId, tenantName);
            
            using var context = _dbContextFactory.CreateDbContext();
            
            var learningPath = await context.LearningPaths.FindAsync(learningPathId);
            if (learningPath == null)
            {
                return NotFound();
            }

            context.LearningPaths.Remove(learningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted learningPath {LearningPathId} for tenant: {TenantName}", learningPathId, tenantName);

            return NoContent();
        }

        private async Task<bool> LearningPathExistsAsync(int learningPathId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.LearningPaths.AnyAsync(e => e.Id == learningPathId);
        }
    }
}