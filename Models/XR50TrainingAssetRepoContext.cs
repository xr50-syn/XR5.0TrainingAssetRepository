
﻿using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGeneration;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class XR50TrainingAssetRepoContext : DbContext
    {
        public XR50TrainingAssetRepoContext(DbContextOptions<XR50TrainingAssetRepoContext> options)
           : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<XR50Tenant>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<XR50Tenant>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<User>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<User>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<TrainingProgram>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<TrainingProgram>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<LearningPath>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<TrainingProgram>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<Material>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<Material>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<WorkflowMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<WorkflowMaterial>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<VideoMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<VideoMaterial>().Property<DateTime>("UpdatedDate");
            modelBuilder.Entity<ChecklistMaterial>().Property<DateTime>("CreatedDate");
            modelBuilder.Entity<ChecklistMaterial>().Property<DateTime>("UpdatedDate");
        }

        public DbSet<XR50Tenant> Tenants { get; set; } = null!;
	    public DbSet<User> Users { get; set; } = null!;
	    public DbSet<TrainingProgram> TrainingPrograms { get; set; } = null!;
        public DbSet<LearningPath> LearningPaths { get; set; } = null!;
	    public DbSet<Material> Materials { get; set; } = null!;
        public DbSet<WorkflowMaterial> Workflows { get; set; } = null!;
        public DbSet<VideoMaterial> Videos { get; set; } = null!;
        public DbSet<ChecklistMaterial> Checklists { get; set; } = null!;
        public DbSet<ImageMaterial> Images { get; set; } = null!;
        public DbSet<PDFMaterial> PDFs { get; set; } = null!;
        public DbSet<UnityDemoMaterial> Demos { get; set; } = null!;
        public DbSet<ChatbotMaterial> Chatbots { get; set; } = null!;
        public DbSet<QuestionnaireMaterial> Questionnaires { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Share> Shares {get; set;} = null!;
        public DbSet<ChecklistEntry> ChecklistEntries { get; set; } = null!;
        public DbSet<VideoTimestamp> VideoTimestamps { get; set; } = null!;
        public DbSet<WorkflowStep> WorkflowSteps { get; set; } = null!;
        public DbSet<QuestionnaireEntry> QuestionnaireEntries { get; set; } = null!;
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
