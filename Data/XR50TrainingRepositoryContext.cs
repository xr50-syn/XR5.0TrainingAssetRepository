using Microsoft.EntityFrameworkCore;
using XR50.Models;
using XR50.Services;

namespace XR50.Data
{
    // XR50 Training Context with Tenant Support
    public class XR50TrainingContext : DbContext
    {
        private readonly IXR50TenantService _tenantService;
        private readonly string _tenantSchema;

        public XR50TrainingContext(DbContextOptions<XR50TrainingContext> options, IXR50TenantService tenantService) 
            : base(options)
        {
            _tenantService = tenantService;
            var tenantName = _tenantService.GetCurrentTenant();
            _tenantSchema = _tenantService.GetTenantSchema(tenantName);
        }

        public DbSet<Program> Programs { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Asset> Assets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Each tenant gets its own schema
            modelBuilder.HasDefaultSchema(_tenantSchema);

            // Configure entities
            ConfigureProgram(modelBuilder);
            ConfigureMaterial(modelBuilder);
            ConfigureAsset(modelBuilder);
        }

        private void ConfigureProgram(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>(entity =>
            {
                entity.ToTable("Programs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
                
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.CreatedAt);
            });
        }

        private void ConfigureMaterial(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Material>(entity =>
            {
                entity.ToTable("Materials");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasColumnType("datetime2");
                entity.Property(e => e.UpdatedAt).HasColumnType("datetime2");
                
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => new { e.Type, e.CreatedAt });
            });
        }

        private void ConfigureAsset(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.ToTable("Assets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Src).IsRequired().HasMaxLength(500);
                
                entity.HasOne(e => e.Material)
                      .WithMany(m => m.Assets)
                      .HasForeignKey(e => e.MaterialsId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                entity.HasIndex(e => e.MaterialsId);
            });
        }
    }
}