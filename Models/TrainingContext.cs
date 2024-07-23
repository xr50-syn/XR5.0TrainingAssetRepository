using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class TrainingContext : DbContext
    {
        public TrainingContext(DbContextOptions<TrainingContext> options)
        : base(options)
        {
        }

        public DbSet<TrainingModule> Trainings { get; set; } = null!;
        public DbSet<XR5_0TrainingRepo.Models.QueryStore> QueryStore { get; set; } = default!;
    }
}
