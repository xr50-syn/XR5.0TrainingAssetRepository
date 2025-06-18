using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Data
{
    // XR50 Training Context with Dynamic Tenant Database Switching
    public class XR50TrainingContext : DbContext
    {
        private readonly IXR50TenantService? _tenantService;
        private readonly IConfiguration? _configuration;

        public XR50TrainingContext(
            DbContextOptions<XR50TrainingContext> options, 
            IXR50TenantService tenantService,
            IConfiguration configuration) 
            : base(options)
        {
            _tenantService = tenantService;
            _configuration = configuration;
        }

        // Constructor for migrations and direct instantiation
        public XR50TrainingContext(DbContextOptions<XR50TrainingContext> options) 
            : base(options)
        {
            // For migrations - services will be null
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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured && _tenantService != null && _configuration != null)
            {
                try
                {
                    // Get the current tenant and build connection string dynamically
                    var currentTenant = _tenantService.GetCurrentTenant();
                    var connectionString = GetTenantConnectionString(currentTenant);
                    
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    }
                }
                catch
                {
                    // If anything fails, don't configure here - let the main registration handle it
                }
            }
        }

        private string GetTenantConnectionString(string tenantName)
        {
            if (_configuration == null) return string.Empty;

            try
            {
                var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
                var baseDatabaseName = _configuration["BaseDatabaseName"] ?? "magical_library";
                
                // For admin operations (tenant management), use base database
                if (tenantName == "default" || string.IsNullOrEmpty(tenantName))
                {
                    return baseConnectionString ?? string.Empty;
                }
                
                // For tenant operations, switch to tenant database
                var tenantDatabase = _tenantService?.GetTenantSchema(tenantName) ?? tenantName;
                return baseConnectionString?.Replace($"Database={baseDatabaseName}", $"Database={tenantDatabase}", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keep it simple - just add the shadow properties like your original
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
            modelBuilder.Entity<ImageMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<ImageMaterial>().Property<DateTime>("UpdatedDate");
        }

        // Keep your existing SaveChanges override
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries().Where(e =>
                e.State == EntityState.Added
                || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                try
                {
                    entityEntry.Property("UpdatedDate").CurrentValue = DateTime.UtcNow;

                    if (entityEntry.State == EntityState.Added)
                    {
                        entityEntry.Property("CreatedDate").CurrentValue = DateTime.UtcNow;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Entity doesn't have shadow properties - skip
                }
            }
        }
    }
}