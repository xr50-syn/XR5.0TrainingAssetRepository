using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public interface ILearningPathService
    {
        Task<IEnumerable<LearningPath>> GetAllLearningPathsAsync();
        Task<LearningPath?> GetLearningPathAsync(int id);
        Task<LearningPath> CreateLearningPathAsync(LearningPath learningPath);
        Task<LearningPath> CreateLearningPathAsync(LearningPath learningPath, IEnumerable<int>? trainingProgramIds = null);
        Task<LearningPath> UpdateLearningPathAsync(LearningPath learningPath);
        Task<bool> DeleteLearningPathAsync(int id);
        Task<bool> LearningPathExistsAsync(int id);
        
        // Junction table operations for Training Program associations
        Task<IEnumerable<LearningPath>> GetLearningPathsByTrainingProgramAsync(int trainingProgramId);
        Task<bool> AssignLearningPathToTrainingProgramAsync(int trainingProgramId, int learningPathId);
        Task<int> AssignMultipleLearningPathsToTrainingProgramAsync(int trainingProgramId, IEnumerable<int> learningPathIds);
        Task<bool> RemoveLearningPathFromTrainingProgramAsync(int trainingProgramId, int learningPathId);
    }

    public class LearningPathService : ILearningPathService
    {
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<LearningPathService> _logger;

        public LearningPathService(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<LearningPathService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<LearningPath>> GetAllLearningPathsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.LearningPaths
                .Include(lp => lp.ProgramLearningPaths)
                    .ThenInclude(plp => plp.TrainingProgram)
                .ToListAsync();
        }

        public async Task<IEnumerable<LearningPath>> GetLearningPathsByTrainingProgramAsync(int trainingProgramId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.ProgramLearningPaths
                .Where(plp => plp.TrainingProgramId == trainingProgramId)
                .Select(plp => plp.LearningPath)
                .ToListAsync();
        }

        public async Task<bool> AssignLearningPathToTrainingProgramAsync(int trainingProgramId, int learningPathId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Check if association already exists
            var existingAssociation = await context.ProgramLearningPaths
                .AnyAsync(plp => plp.TrainingProgramId == trainingProgramId && plp.LearningPathId == learningPathId);
            
            if (existingAssociation)
            {
                _logger.LogWarning("Association between training program {TrainingProgramId} and learning path {LearningPathId} already exists", 
                    trainingProgramId, learningPathId);
                return false;
            }

            var programLearningPath = new ProgramLearningPath
            {
                TrainingProgramId = trainingProgramId,
                LearningPathId = learningPathId
            };

            context.ProgramLearningPaths.Add(programLearningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Associated learning path {LearningPathId} with training program {TrainingProgramId}", 
                learningPathId, trainingProgramId);
            
            return true;
        }

        public async Task<int> AssignMultipleLearningPathsToTrainingProgramAsync(int trainingProgramId, IEnumerable<int> learningPathIds)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var associationsToAdd = new List<ProgramLearningPath>();
            
            foreach (var learningPathId in learningPathIds)
            {
                // Check if association already exists
                var existingAssociation = await context.ProgramLearningPaths
                    .AnyAsync(plp => plp.TrainingProgramId == trainingProgramId && plp.LearningPathId == learningPathId);
                
                if (!existingAssociation)
                {
                    // Verify learning path exists
                    var learningPathExists = await context.LearningPaths.AnyAsync(lp => lp.Id == learningPathId);
                    if (learningPathExists)
                    {
                        associationsToAdd.Add(new ProgramLearningPath
                        {
                            TrainingProgramId = trainingProgramId,
                            LearningPathId = learningPathId
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Learning path {LearningPathId} not found, skipping association", learningPathId);
                    }
                }
                else
                {
                    _logger.LogDebug("Association between training program {TrainingProgramId} and learning path {LearningPathId} already exists", 
                        trainingProgramId, learningPathId);
                }
            }

            if (associationsToAdd.Any())
            {
                context.ProgramLearningPaths.AddRange(associationsToAdd);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Created {Count} new associations for training program {TrainingProgramId}", 
                    associationsToAdd.Count, trainingProgramId);
            }

            return associationsToAdd.Count;
        }

        public async Task<bool> RemoveLearningPathFromTrainingProgramAsync(int trainingProgramId, int learningPathId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var programLearningPath = await context.ProgramLearningPaths
                .FirstOrDefaultAsync(plp => plp.TrainingProgramId == trainingProgramId && plp.LearningPathId == learningPathId);
            
            if (programLearningPath == null)
            {
                return false;
            }

            context.ProgramLearningPaths.Remove(programLearningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed learning path {LearningPathId} from training program {TrainingProgramId}", 
                learningPathId, trainingProgramId);
            
            return true;
        }

        public async Task<LearningPath?> GetLearningPathAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.LearningPaths
                .Include(lp => lp.ProgramLearningPaths)
                    .ThenInclude(plp => plp.TrainingProgram)
                .FirstOrDefaultAsync(lp => lp.Id == id);
        }

        public async Task<LearningPath> CreateLearningPathAsync(LearningPath learningPath)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            context.LearningPaths.Add(learningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created learning path: {Name} with ID: {Id}", 
                learningPath.LearningPathName, learningPath.Id);
            
            return learningPath;
        }

        public async Task<LearningPath> CreateLearningPathAsync(LearningPath learningPath, IEnumerable<int>? trainingProgramIds = null)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Create the learning path first
            context.LearningPaths.Add(learningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created learning path: {Name} with ID: {Id}", 
                learningPath.LearningPathName, learningPath.Id);

            // If training program IDs are provided, create associations
            if (trainingProgramIds != null && trainingProgramIds.Any())
            {
                var associations = new List<ProgramLearningPath>();
                foreach (var trainingProgramId in trainingProgramIds)
                {
                    // Verify training program exists
                    var programExists = await context.TrainingPrograms.AnyAsync(tp => tp.Id == trainingProgramId);
                    if (programExists)
                    {
                        associations.Add(new ProgramLearningPath
                        {
                            TrainingProgramId = trainingProgramId,
                            LearningPathId = learningPath.Id
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Training program {TrainingProgramId} not found, skipping association", trainingProgramId);
                    }
                }

                if (associations.Any())
                {
                    context.ProgramLearningPaths.AddRange(associations);
                    await context.SaveChangesAsync();
                    
                    _logger.LogInformation("Created {Count} associations for learning path {LearningPathId}", 
                        associations.Count, learningPath.Id);
                }
            }
            
            return learningPath;
        }

        public async Task<LearningPath> UpdateLearningPathAsync(LearningPath learningPath)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(learningPath).State = EntityState.Modified;
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated learning path: {Id}", learningPath.Id);
            
            return learningPath;
        }

        public async Task<bool> DeleteLearningPathAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var learningPath = await context.LearningPaths.FindAsync(id);
            if (learningPath == null)
            {
                return false;
            }

            context.LearningPaths.Remove(learningPath);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted learning path: {Id}", id);
            
            return true;
        }

        public async Task<bool> LearningPathExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.LearningPaths.AnyAsync(e => e.Id == id);
        }
    }
}