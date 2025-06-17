using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Data
{
    // XR50 Training Context with Tenant Support (MySQL Compatible)
    public class XR50TrainingContext : DbContext
    {
        private readonly IXR50TenantService _tenantService;

        public XR50TrainingContext(DbContextOptions<XR50TrainingContext> options, IXR50TenantService tenantService) 
            : base(options)
        {
            _tenantService = tenantService;
            // No schema setting for MySQL - each tenant gets separate database
        }

        // All your existing DbSets
        public DbSet<XR50Tenant> Tenants { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<TrainingProgram> TrainingPrograms { get; set; } = null!;
        public DbSet<LearningPath> LearningPaths { get; set; } = null!;
        public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<WorkflowMaterial> Workflows { get; set; } = null!;
        public DbSet<VideoMaterial> Videos { get; set; } = null!;
        public DbSet<ChecklistMaterial> Checklists { get; set; } = null!;
        public DbSet<ImageMaterial> Images { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Share> Shares { get; set; } = null!;
        public DbSet<ChecklistEntry> ChecklistEntries { get; set; } = null!;
        public DbSet<VideoTimestamp> VideoTimestamps { get; set; } = null!;
        public DbSet<WorkflowStep> WorkflowSteps { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // No schema setting for MySQL - keep your existing shadow properties
            modelBuilder.Entity<XR50Tenant>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<XR50Tenant>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<User>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<User>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<TrainingProgram>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<TrainingProgram>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<LearningPath>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<LearningPath>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<Material>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<Material>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<WorkflowMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<WorkflowMaterial>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<VideoMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<VideoMaterial>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<ChecklistMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<ChecklistMaterial>().Property<DateTime>("UpdatedDate");
        }

        // Keep your existing SaveChanges override
        public override int SaveChanges()
        {
            var entries = ChangeTracker.Entries().Where(e =>
                e.State == EntityState.Added
                || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                entityEntry.Property("UpdatedDate").CurrentValue = DateTime.Now;

                if (entityEntry.State == EntityState.Added)
                {
                    entityEntry.Property("CreatedDate").CurrentValue = DateTime.Now;
                }
            }
            return base.SaveChanges();
        }
    }
}
