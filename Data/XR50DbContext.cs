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
        public DbSet<ProgramMaterial> ProgramMaterials { get; set; } = null!;
        public DbSet<ProgramLearningPath> ProgramLearningPaths { get; set; } = null!;
        public DbSet<GroupUser> GroupUsers { get; set; } = null!;
        public DbSet<TenantAdmin> TenantAdmins { get; set; } = null!;
        public DbSet<MaterialRelationship> MaterialRelationships { get; set; } = null!;
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
                        
                        // Log the actual database being used
                        var logger = _configuration.GetSection("Logging").Get<object>(); // Basic logging check
                        Console.WriteLine($" DbContext configured for tenant: {currentTenant}, connection: {connectionString.Replace("Password=", "Password=***")}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error configuring DbContext: {ex.Message}");
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
                    Console.WriteLine($"🔄 Using default database for tenant: {tenantName}");
                    return baseConnectionString ?? string.Empty;
                }
                
                // For tenant operations, switch to tenant database
                var tenantDatabase = _tenantService?.GetTenantSchema(tenantName) ?? tenantName;
                var tenantConnectionString = baseConnectionString?.Replace($"database={baseDatabaseName}", $"database={tenantDatabase}", StringComparison.OrdinalIgnoreCase) ?? string.Empty;
                
                Console.WriteLine($" Switching to tenant database: {tenantDatabase} for tenant: {tenantName}");
                
                return tenantConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building tenant connection string: {ex.Message}");
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

            // Just configure composite primary keys - EF will figure out the relationships
            modelBuilder.Entity<ProgramMaterial>()
                .HasKey(pm => new { pm.TrainingProgramId, pm.MaterialId });

            modelBuilder.Entity<ProgramLearningPath>()
                .HasKey(plp => new { plp.TrainingProgramId, plp.LearningPathId });

            modelBuilder.Entity<GroupUser>()
                .HasKey(gu => new { gu.GroupName, gu.UserName });

            modelBuilder.Entity<TenantAdmin>()
                .HasKey(ta => new { ta.TenantName, ta.UserName });

            // MaterialRelationship has its own GUID primary key
            modelBuilder.Entity<MaterialRelationship>()
                .HasKey(mr => mr.Id);

            // Add indexes for performance
            modelBuilder.Entity<MaterialRelationship>()
                .HasIndex(mr => mr.MaterialId);
            
            modelBuilder.Entity<MaterialRelationship>()
                .HasIndex(mr => new { mr.RelatedEntityId, mr.RelatedEntityType });

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