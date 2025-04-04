using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class XR50RepoContext : DbContext
    {
        public XR50RepoContext(DbContextOptions<XR50RepoContext> options)
           : base(options)
        {
        }

        public DbSet<XR50Tennant> Tennants { get; set; } = null!;
	    public DbSet<User> Users { get; set; } = null!;
	    public DbSet<TrainingModule> Trainings { get; set; } = null!;
	    public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<WorkflowMaterial> Workflows { get; set; } = null!;
        public DbSet<VideoMaterial> Videos { get; set; } = null!;
        public DbSet<ChecklistMaterial> Checklists { get; set; } = null!;
        public DbSet<ImageMaterial> Images { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Share> Shares {get; set;} = null!;
        public DbSet<ChecklistEntry> ChecklistEntries { get; set; } = null!;
        public DbSet<VideoTimestamp> VideoTimestamps { get; set; } = null!;
        public DbSet<WorkflowStep> WorkflowSteps { get; set; } = null!;

    }
}
