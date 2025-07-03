using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public interface ITrainingProgramService
    {
        Task<IEnumerable<TrainingProgram>> GetAllTrainingProgramsAsync();
        Task<TrainingProgram?> GetTrainingProgramAsync(int id);
        Task<TrainingProgram> CreateTrainingProgramAsync(TrainingProgram program);
        Task<TrainingProgram> UpdateTrainingProgramAsync(TrainingProgram program);
        Task<bool> DeleteTrainingProgramAsync(int id);
        Task<bool> TrainingProgramExistsAsync(int id);
    }

    public class TrainingProgramService : ITrainingProgramService
    {
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<TrainingProgramService> _logger;

        public TrainingProgramService(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<TrainingProgramService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<TrainingProgram>> GetAllTrainingProgramsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms
                .Include(tp => tp.ProgramLearningPaths)  // Show learning path associations
                .Include(tp => tp.ProgramMaterials)      // Show material associations (if you have this)
                .ToListAsync();
        }

        public async Task<TrainingProgram?> GetTrainingProgramAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms
                .Include(tp => tp.ProgramLearningPaths)  // Show learning path associations
                .Include(tp => tp.ProgramMaterials)      // Show material associations (if you have this)
                .FirstOrDefaultAsync(tp => tp.Id == id);
        }
        public async Task<TrainingProgram> CreateTrainingProgramAsync(TrainingProgram program)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Set creation timestamp if not already set
            if (string.IsNullOrEmpty(program.Created_at))
            {
                program.Created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            context.TrainingPrograms.Add(program);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created training program: {Name} with ID: {Id}", program.Name, program.Id);
            
            return program;
        }

        public async Task<TrainingProgram> UpdateTrainingProgramAsync(TrainingProgram program)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            context.Entry(program).State = EntityState.Modified;
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated training program: {Id}", program.Id);
            
            return program;
        }

        public async Task<bool> DeleteTrainingProgramAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var program = await context.TrainingPrograms.FindAsync(id);
            if (program == null)
            {
                return false;
            }

            context.TrainingPrograms.Remove(program);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted training program: {Id}", id);
            
            return true;
        }

        public async Task<bool> TrainingProgramExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.TrainingPrograms.AnyAsync(e => e.Id == id);
        }
    }
}