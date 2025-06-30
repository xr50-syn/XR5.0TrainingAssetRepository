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
        public DbSet<QuestionnaireEntry> QuestionnaireEntries { get; set; } = null!;
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
            // Configure TPH inheritance for Material hierarchy
            modelBuilder.Entity<Material>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Material>("Material")
                .HasValue<VideoMaterial>("VideoMaterial")
                .HasValue<ImageMaterial>("ImageMaterial")
                .HasValue<ChecklistMaterial>("ChecklistMaterial")
                .HasValue<WorkflowMaterial>("WorkflowMaterial")
                .HasValue<PDFMaterial>("PDFMaterial")
                .HasValue<UnityDemoMaterial>("UnityDemoMaterial")
                .HasValue<ChatbotMaterial>("ChatbotMaterial")
                .HasValue<QuestionnaireMaterial>("QuestionnaireMaterial")
                .HasValue<MQTT_TemplateMaterial>("MQTT_TemplateMaterial")
                .HasValue<DefaultMaterial>("DefaultMaterial");

            // Configure specific properties for MQTT_TemplateMaterial
            modelBuilder.Entity<MQTT_TemplateMaterial>()
                .Property(m => m.message_type)
                .HasColumnName("message_type");
                
            modelBuilder.Entity<MQTT_TemplateMaterial>()
                .Property(m => m.message_text)
                .HasColumnName("message_text");

            // Configure properties for VideoMaterial
            modelBuilder.Entity<VideoMaterial>()
                .Property(m => m.AssetId)
                .HasColumnName("AssetId");
            modelBuilder.Entity<VideoMaterial>()
                .Property(m => m.VideoPath)
                .HasColumnName("VideoPath");
            modelBuilder.Entity<VideoMaterial>()
                .Property(m => m.VideoDuration)
                .HasColumnName("VideoDuration");
            modelBuilder.Entity<VideoMaterial>()
                .Property(m => m.VideoResolution)
                .HasColumnName("VideoResolution");

            // Configure properties for ImageMaterial
            modelBuilder.Entity<ImageMaterial>()
                .Property(m => m.AssetId)
                .HasColumnName("AssetId");
            modelBuilder.Entity<ImageMaterial>()
                .Property(m => m.ImagePath)
                .HasColumnName("ImagePath");
            modelBuilder.Entity<ImageMaterial>()
                .Property(m => m.ImageWidth)
                .HasColumnName("ImageWidth");
            modelBuilder.Entity<ImageMaterial>()
                .Property(m => m.ImageHeight)
                .HasColumnName("ImageHeight");
            modelBuilder.Entity<ImageMaterial>()
                .Property(m => m.ImageFormat)
                .HasColumnName("ImageFormat");

            // Configure properties for PDFMaterial
            modelBuilder.Entity<PDFMaterial>()
                .Property(m => m.AssetId)
                .HasColumnName("AssetId");
            modelBuilder.Entity<PDFMaterial>()
                .Property(m => m.PdfPath)
                .HasColumnName("PdfPath");
            modelBuilder.Entity<PDFMaterial>()
                .Property(m => m.PdfPageCount)
                .HasColumnName("PdfPageCount");
            modelBuilder.Entity<PDFMaterial>()
                .Property(m => m.PdfFileSize)
                .HasColumnName("PdfFileSize");

            // Configure properties for ChatbotMaterial
            modelBuilder.Entity<ChatbotMaterial>()
                .Property(m => m.ChatbotConfig)
                .HasColumnName("ChatbotConfig");
            modelBuilder.Entity<ChatbotMaterial>()
                .Property(m => m.ChatbotModel)
                .HasColumnName("ChatbotModel");
            modelBuilder.Entity<ChatbotMaterial>()
                .Property(m => m.ChatbotPrompt)
                .HasColumnName("ChatbotPrompt");

            // Configure properties for QuestionnaireMaterial
            modelBuilder.Entity<QuestionnaireMaterial>()
                .Property(m => m.QuestionnaireConfig)
                .HasColumnName("QuestionnaireConfig");
            modelBuilder.Entity<QuestionnaireMaterial>()
                .Property(m => m.QuestionnaireType)
                .HasColumnName("QuestionnaireType");
            modelBuilder.Entity<QuestionnaireMaterial>()
                .Property(m => m.PassingScore)
                .HasColumnName("PassingScore");

            // Configure properties for UnityDemoMaterial
            modelBuilder.Entity<UnityDemoMaterial>()
                .Property(m => m.AssetId)
                .HasColumnName("AssetId");
            modelBuilder.Entity<UnityDemoMaterial>()
                .Property(m => m.UnityVersion)
                .HasColumnName("UnityVersion");
            modelBuilder.Entity<UnityDemoMaterial>()
                .Property(m => m.UnityBuildTarget)
                .HasColumnName("UnityBuildTarget");
            modelBuilder.Entity<UnityDemoMaterial>()
                .Property(m => m.UnitySceneName)
                .HasColumnName("UnitySceneName");
                
            // Configure AssetId for DefaultMaterial
            modelBuilder.Entity<DefaultMaterial>()
                .Property(m => m.AssetId)
                .HasColumnName("AssetId");

            // Configure relationships for child entities (separate tables)
            modelBuilder.Entity<VideoMaterial>()
                .HasMany(v => v.VideoTimestamps)
                .WithOne()
                .HasForeignKey("VideoMaterialId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChecklistMaterial>()
                .HasMany(c => c.ChecklistEntries)
                .WithOne()
                .HasForeignKey("ChecklistMaterialId")
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkflowMaterial>()
                .HasMany(w => w.WorkflowSteps)
                .WithOne()
                .HasForeignKey("WorkflowMaterialId")
                .OnDelete(DeleteBehavior.Cascade);

            // Configure composite primary keys for junction tables
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

            // Add foreign key properties to child tables
            modelBuilder.Entity<VideoTimestamp>()
                .Property<int?>("VideoMaterialId");
            
            modelBuilder.Entity<ChecklistEntry>()
                .Property<int?>("ChecklistMaterialId");
            
            modelBuilder.Entity<WorkflowStep>()
                .Property<int?>("WorkflowMaterialId");
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
                e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                var entity = entityEntry.Entity;
                
                // Handle Material entities (which have Created_at and Updated_at properties)
                if (entity is Material material)
                {
                    material.Updated_at = DateTime.UtcNow;
                    
                    if (entityEntry.State == EntityState.Added)
                    {
                        material.Created_at = DateTime.UtcNow;
                    }
                    continue; // Skip shadow property logic for Materials
                }
                
                // Handle TrainingProgram entities (which have Created_at property)
                if (entity is TrainingProgram program)
                {
                    if (entityEntry.State == EntityState.Added && string.IsNullOrEmpty(program.Created_at))
                    {
                        program.Created_at = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    continue; // Skip shadow property logic for TrainingPrograms
                }
                
                // For entities that still use shadow properties (if any)
                try
                {
                    // Check if the entity has shadow properties before trying to update them
                    var entityType = entityEntry.Metadata;
                    var updatedDateProperty = entityType.FindProperty("UpdatedDate");
                    var createdDateProperty = entityType.FindProperty("CreatedDate");
                    
                    if (updatedDateProperty != null)
                    {
                        entityEntry.Property("UpdatedDate").CurrentValue = DateTime.UtcNow;
                    }

                    if (createdDateProperty != null && entityEntry.State == EntityState.Added)
                    {
                        entityEntry.Property("CreatedDate").CurrentValue = DateTime.UtcNow;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Entity doesn't have shadow properties - this is expected for most entities now
                    // We can safely ignore this or add specific logging if needed
                }
            }
        
        }
    }
}