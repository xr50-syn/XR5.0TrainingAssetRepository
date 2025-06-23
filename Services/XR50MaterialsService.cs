using Microsoft.EntityFrameworkCore;
using XR50TrainingAssetRepo.Models;
using XR50TrainingAssetRepo.Services;

namespace XR50TrainingAssetRepo.Services
{
    public interface IMaterialService
    {
        // Base Material Operations
        Task<IEnumerable<Material>> GetAllMaterialsAsync();
        Task<Material?> GetMaterialAsync(int id);
        Task<Material> CreateMaterialAsync(Material material);
        Task<Material> UpdateMaterialAsync(Material material);
        Task<bool> DeleteMaterialAsync(int id);
        Task<bool> MaterialExistsAsync(int id);
        
        // Material Type-Specific Operations
        Task<IEnumerable<Material>> GetMaterialsByTypeAsync(System.Type materialType);
        Task<IEnumerable<VideoMaterial>> GetAllVideoMaterialsAsync();
        Task<IEnumerable<ChecklistMaterial>> GetAllChecklistMaterialsAsync();
        Task<IEnumerable<WorkflowMaterial>> GetAllWorkflowMaterialsAsync();
        Task<IEnumerable<ImageMaterial>> GetAllImageMaterialsAsync();
        
        // Video Material Specific
        Task<VideoMaterial?> GetVideoMaterialWithTimestampsAsync(int id);
        Task<VideoMaterial> AddTimestampToVideoAsync(int videoId, VideoTimestamp timestamp);
        Task<bool> RemoveTimestampFromVideoAsync(int videoId, int timestampId);
        
        // Checklist Material Specific
        Task<ChecklistMaterial?> GetChecklistMaterialWithEntriesAsync(int id);
        Task<ChecklistMaterial> AddEntryToChecklistAsync(int checklistId, ChecklistEntry entry);
        Task<bool> RemoveEntryFromChecklistAsync(int checklistId, int entryId);
        
        // Workflow Material Specific
        Task<WorkflowMaterial?> GetWorkflowMaterialWithStepsAsync(int id);
        Task<WorkflowMaterial> AddStepToWorkflowAsync(int workflowId, WorkflowStep step);
        Task<bool> RemoveStepFromWorkflowAsync(int workflowId, int stepId);
        
        // Direct Asset Relationships (Many-to-One, only for certain material types)
        Task<IEnumerable<Material>> GetMaterialsByAssetIdAsync(string assetId);
        Task<bool> AssignAssetToMaterialAsync(int materialId, string assetId);
        Task<bool> RemoveAssetFromMaterialAsync(int materialId);
        Task<string?> GetMaterialAssetIdAsync(int materialId);
        
        // Polymorphic Relationships via MaterialRelationships Table
        Task<MaterialRelationship> CreateRelationshipAsync(MaterialRelationship relationship);
        Task<bool> DeleteRelationshipAsync(string relationshipId);
        Task<IEnumerable<MaterialRelationship>> GetMaterialRelationshipsAsync(int materialId);
        Task<IEnumerable<MaterialRelationship>> GetRelationshipsByTypeAsync(int materialId, string relatedEntityType);
        
        // Learning Path Associations
        Task<string> AssignMaterialToLearningPathAsync(int materialId, int learningPathId, string relationshipType = "contains", int? displayOrder = null);
        Task<bool> RemoveMaterialFromLearningPathAsync(int materialId, int learningPathId);
        Task<IEnumerable<Material>> GetMaterialsByLearningPathAsync(int learningPathId, bool includeOrder = true);
        Task<bool> ReorderMaterialsInLearningPathAsync(int learningPathId, Dictionary<int, int> materialOrderMap);
        
        // Training Program Direct Associations  
        Task<string> AssignMaterialToTrainingProgramAsync(int materialId, int trainingProgramId, string relationshipType = "assigned");
        Task<bool> RemoveMaterialFromTrainingProgramAsync(int materialId, int trainingProgramId);
        Task<IEnumerable<Material>> GetMaterialsByTrainingProgramAsync(int trainingProgramId);
        
        // Material Dependencies
        Task<string> CreateMaterialDependencyAsync(int materialId, int prerequisiteMaterialId, string relationshipType = "prerequisite");
        Task<bool> RemoveMaterialDependencyAsync(int materialId, int prerequisiteMaterialId);
        Task<IEnumerable<Material>> GetMaterialPrerequisitesAsync(int materialId);
        Task<IEnumerable<Material>> GetMaterialDependentsAsync(int materialId);
    }

    public class MaterialService : IMaterialService
    {
        private readonly IXR50TenantDbContextFactory _dbContextFactory;
        private readonly ILogger<MaterialService> _logger;

        public MaterialService(
            IXR50TenantDbContextFactory dbContextFactory,
            ILogger<MaterialService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        #region Base Material Operations

        public async Task<IEnumerable<Material>> GetAllMaterialsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Materials.ToListAsync();
        }

        public async Task<Material?> GetMaterialAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Materials.FindAsync(id);
        }

        public async Task<Material> CreateMaterialAsync(Material material)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Set timestamps
            material.Created_at = DateTime.UtcNow;
            material.Updated_at = DateTime.UtcNow;
            
            context.Materials.Add(material);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created material: {Name} (Type: {Type}) with ID: {Id}", 
                material.Name, material.GetType().Name, material.Id);
            
            return material;
        }

        public async Task<Material> UpdateMaterialAsync(Material material)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            material.Updated_at = DateTime.UtcNow;
            context.Entry(material).State = EntityState.Modified;
            await context.SaveChangesAsync();

            _logger.LogInformation("Updated material: {Id} (Type: {Type})", 
                material.Id, material.GetType().Name);
            
            return material;
        }

        public async Task<bool> DeleteMaterialAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var material = await context.Materials.FindAsync(id);
            if (material == null)
            {
                return false;
            }

            // Also delete all relationships involving this material
            var relationships = await context.MaterialRelationships
                .Where(mr => mr.MaterialId == id || 
                            (mr.RelatedEntityType == "Material" && mr.RelatedEntityId == id.ToString()))
                .ToListAsync();
            
            context.MaterialRelationships.RemoveRange(relationships);
            context.Materials.Remove(material);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted material: {Id} (Type: {Type}) and {RelationshipCount} relationships", 
                id, material.GetType().Name, relationships.Count);
            
            return true;
        }

        public async Task<bool> MaterialExistsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Materials.AnyAsync(e => e.Id == id);
        }

        #endregion

        #region Material Type-Specific Operations

        public async Task<IEnumerable<Material>> GetMaterialsByTypeAsync(System.Type materialType)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var discriminator = materialType.Name;
            return await context.Materials
                .Where(m => EF.Property<string>(m, "Discriminator") == discriminator)
                .ToListAsync();
        }

        public async Task<IEnumerable<VideoMaterial>> GetAllVideoMaterialsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Videos.ToListAsync();
        }

        public async Task<IEnumerable<ChecklistMaterial>> GetAllChecklistMaterialsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Checklists.ToListAsync();
        }

        public async Task<IEnumerable<WorkflowMaterial>> GetAllWorkflowMaterialsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Workflows.ToListAsync();
        }

        public async Task<IEnumerable<ImageMaterial>> GetAllImageMaterialsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            return await context.Images.ToListAsync();
        }

        #endregion

        #region Video Material Specific

        public async Task<VideoMaterial?> GetVideoMaterialWithTimestampsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Videos
                .Include(v => v.VideoTimestamps)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<VideoMaterial> AddTimestampToVideoAsync(int videoId, VideoTimestamp timestamp)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var video = await context.Videos.FindAsync(videoId);
            if (video == null)
            {
                throw new ArgumentException($"Video material with ID {videoId} not found");
            }

            context.VideoTimestamps.Add(timestamp);
            await context.SaveChangesAsync();

            _logger.LogInformation("Added timestamp '{Title}' to video material {VideoId}", 
                timestamp.Title, videoId);
            
            return video;
        }

        public async Task<bool> RemoveTimestampFromVideoAsync(int videoId, int timestampId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var timestamp = await context.VideoTimestamps.FindAsync(timestampId);
            if (timestamp == null)
            {
                return false;
            }

            context.VideoTimestamps.Remove(timestamp);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed timestamp {TimestampId} from video material {VideoId}", 
                timestampId, videoId);
            
            return true;
        }

        #endregion

        #region Checklist Material Specific

        public async Task<ChecklistMaterial?> GetChecklistMaterialWithEntriesAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Checklists
                .Include(c => c.ChecklistEntries)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ChecklistMaterial> AddEntryToChecklistAsync(int checklistId, ChecklistEntry entry)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var checklist = await context.Checklists.FindAsync(checklistId);
            if (checklist == null)
            {
                throw new ArgumentException($"Checklist material with ID {checklistId} not found");
            }

            context.ChecklistEntries.Add(entry);
            await context.SaveChangesAsync();

            _logger.LogInformation("Added entry '{Text}' to checklist material {ChecklistId}", 
                entry.Text, checklistId);
            
            return checklist;
        }

        public async Task<bool> RemoveEntryFromChecklistAsync(int checklistId, int entryId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var entry = await context.ChecklistEntries.FindAsync(entryId);
            if (entry == null)
            {
                return false;
            }

            context.ChecklistEntries.Remove(entry);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed entry {EntryId} from checklist material {ChecklistId}", 
                entryId, checklistId);
            
            return true;
        }

        #endregion

        #region Workflow Material Specific

        public async Task<WorkflowMaterial?> GetWorkflowMaterialWithStepsAsync(int id)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.Workflows
                .Include(w => w.WorkflowSteps)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<WorkflowMaterial> AddStepToWorkflowAsync(int workflowId, WorkflowStep step)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var workflow = await context.Workflows.FindAsync(workflowId);
            if (workflow == null)
            {
                throw new ArgumentException($"Workflow material with ID {workflowId} not found");
            }

            context.WorkflowSteps.Add(step);
            await context.SaveChangesAsync();

            _logger.LogInformation("Added step '{Title}' to workflow material {WorkflowId}", 
                step.Title, workflowId);
            
            return workflow;
        }

        public async Task<bool> RemoveStepFromWorkflowAsync(int workflowId, int stepId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var step = await context.WorkflowSteps.FindAsync(stepId);
            if (step == null)
            {
                return false;
            }

            context.WorkflowSteps.Remove(step);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed step {StepId} from workflow material {WorkflowId}", 
                stepId, workflowId);
            
            return true;
        }

        #endregion

        #region Direct Asset Relationships (Many-to-One, only for certain material types)

        public async Task<IEnumerable<Material>> GetMaterialsByAssetIdAsync(string assetId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            // Only certain material types have AssetId field
            return await context.Materials
                .Where(m => EF.Property<string>(m, "AssetId") == assetId &&
                           (EF.Property<string>(m, "Discriminator") == "VideoMaterial" ||
                            EF.Property<string>(m, "Discriminator") == "ImageMaterial" ||
                            EF.Property<string>(m, "Discriminator") == "UnityDemoMaterial" ||
                            EF.Property<string>(m, "Discriminator") == "DefaultMaterial"))
                .ToListAsync();
        }

        public async Task<bool> AssignAssetToMaterialAsync(int materialId, string assetId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var material = await context.Materials.FindAsync(materialId);
            if (material == null)
            {
                return false;
            }

            // Check if this material type supports assets
            var discriminator = context.Entry(material).Property("Discriminator").CurrentValue?.ToString();
            var supportsAssets = discriminator == "VideoMaterial" || 
                               discriminator == "ImageMaterial" || 
                               discriminator == "UnityDemoMaterial" || 
                               discriminator == "DefaultMaterial";

            if (!supportsAssets)
            {
                _logger.LogWarning("Material type {Discriminator} does not support asset assignment", discriminator);
                return false;
            }

            context.Entry(material).Property("AssetId").CurrentValue = assetId;
            material.Updated_at = DateTime.UtcNow;
            
            await context.SaveChangesAsync();

            _logger.LogInformation("Assigned asset {AssetId} to material {MaterialId} (Type: {Type})", 
                assetId, materialId, discriminator);
            
            return true;
        }

        public async Task<bool> RemoveAssetFromMaterialAsync(int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var material = await context.Materials.FindAsync(materialId);
            if (material == null)
            {
                return false;
            }

            // Check if this material type supports assets
            var discriminator = context.Entry(material).Property("Discriminator").CurrentValue?.ToString();
            var supportsAssets = discriminator == "VideoMaterial" || 
                               discriminator == "ImageMaterial" || 
                               discriminator == "UnityDemoMaterial" || 
                               discriminator == "DefaultMaterial";

            if (!supportsAssets)
            {
                _logger.LogWarning("Material type {Discriminator} does not support assets", discriminator);
                return false;
            }

            context.Entry(material).Property("AssetId").CurrentValue = null;
            material.Updated_at = DateTime.UtcNow;
            
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed asset from material {MaterialId} (Type: {Type})", 
                materialId, discriminator);
            
            return true;
        }

        public async Task<string?> GetMaterialAssetIdAsync(int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var material = await context.Materials.FindAsync(materialId);
            if (material == null)
            {
                return null;
            }

            // Check if this material type supports assets
            var discriminator = context.Entry(material).Property("Discriminator").CurrentValue?.ToString();
            var supportsAssets = discriminator == "VideoMaterial" || 
                               discriminator == "ImageMaterial" || 
                               discriminator == "UnityDemoMaterial" || 
                               discriminator == "DefaultMaterial";

            if (!supportsAssets)
            {
                return null;
            }

            return context.Entry(material).Property("AssetId").CurrentValue?.ToString();
        }

        #endregion

        #region Polymorphic Relationships via MaterialRelationships

        public async Task<MaterialRelationship> CreateRelationshipAsync(MaterialRelationship relationship)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            relationship.Id = Guid.NewGuid().ToString();
            
            context.MaterialRelationships.Add(relationship);
            await context.SaveChangesAsync();

            _logger.LogInformation("Created relationship {RelationshipId}: Material {MaterialId} → {RelatedEntityType} {RelatedEntityId} ({RelationshipType})",
                relationship.Id, relationship.MaterialId, relationship.RelatedEntityType, 
                relationship.RelatedEntityId, relationship.RelationshipType);
            
            return relationship;
        }

        public async Task<bool> DeleteRelationshipAsync(string relationshipId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var relationship = await context.MaterialRelationships.FindAsync(relationshipId);
            if (relationship == null)
            {
                return false;
            }

            context.MaterialRelationships.Remove(relationship);
            await context.SaveChangesAsync();

            _logger.LogInformation("Deleted relationship {RelationshipId}", relationshipId);
            
            return true;
        }

        public async Task<IEnumerable<MaterialRelationship>> GetMaterialRelationshipsAsync(int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.MaterialRelationships
                .Where(mr => mr.MaterialId == materialId)
                .OrderBy(mr => mr.DisplayOrder ?? int.MaxValue)
                .ToListAsync();
        }

        public async Task<IEnumerable<MaterialRelationship>> GetRelationshipsByTypeAsync(int materialId, string relatedEntityType)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await context.MaterialRelationships
                .Where(mr => mr.MaterialId == materialId && mr.RelatedEntityType == relatedEntityType)
                .OrderBy(mr => mr.DisplayOrder ?? int.MaxValue)
                .ToListAsync();
        }

        #endregion

        #region Learning Path Associations

        public async Task<string> AssignMaterialToLearningPathAsync(int materialId, int learningPathId, string relationshipType = "contains", int? displayOrder = null)
        {
            var relationship = new MaterialRelationship
            {
                MaterialId = materialId,
                RelatedEntityId = learningPathId.ToString(),
                RelatedEntityType = "LearningPath",
                RelationshipType = relationshipType,
                DisplayOrder = displayOrder
            };

            var created = await CreateRelationshipAsync(relationship);
            return created.Id;
        }

        public async Task<bool> RemoveMaterialFromLearningPathAsync(int materialId, int learningPathId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var relationship = await context.MaterialRelationships
                .FirstOrDefaultAsync(mr => mr.MaterialId == materialId && 
                                         mr.RelatedEntityType == "LearningPath" &&
                                         mr.RelatedEntityId == learningPathId.ToString());
            
            if (relationship == null)
            {
                return false;
            }

            context.MaterialRelationships.Remove(relationship);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed material {MaterialId} from learning path {LearningPathId}", 
                materialId, learningPathId);
            
            return true;
        }

        public async Task<IEnumerable<Material>> GetMaterialsByLearningPathAsync(int learningPathId, bool includeOrder = true)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var query = from mr in context.MaterialRelationships
                       join m in context.Materials on mr.MaterialId equals m.Id
                       where mr.RelatedEntityType == "LearningPath" && 
                             mr.RelatedEntityId == learningPathId.ToString()
                       select new { Material = m, Relationship = mr };

            if (includeOrder)
            {
                query = query.OrderBy(x => x.Relationship.DisplayOrder ?? int.MaxValue);
            }

            var results = await query.ToListAsync();
            return results.Select(r => r.Material);
        }

        public async Task<bool> ReorderMaterialsInLearningPathAsync(int learningPathId, Dictionary<int, int> materialOrderMap)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var relationships = await context.MaterialRelationships
                .Where(mr => mr.RelatedEntityType == "LearningPath" && 
                           mr.RelatedEntityId == learningPathId.ToString())
                .ToListAsync();

            foreach (var relationship in relationships)
            {
                if (materialOrderMap.TryGetValue(relationship.MaterialId, out int newOrder))
                {
                    relationship.DisplayOrder = newOrder;
                }
            }

            await context.SaveChangesAsync();

            _logger.LogInformation("Reordered {Count} materials in learning path {LearningPathId}", 
                materialOrderMap.Count, learningPathId);
            
            return true;
        }

        #endregion

        #region Training Program Direct Associations

        public async Task<string> AssignMaterialToTrainingProgramAsync(int materialId, int trainingProgramId, string relationshipType = "assigned")
        {
            var relationship = new MaterialRelationship
            {
                MaterialId = materialId,
                RelatedEntityId = trainingProgramId.ToString(),
                RelatedEntityType = "TrainingProgram",
                RelationshipType = relationshipType
            };

            var created = await CreateRelationshipAsync(relationship);
            return created.Id;
        }

        public async Task<bool> RemoveMaterialFromTrainingProgramAsync(int materialId, int trainingProgramId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var relationship = await context.MaterialRelationships
                .FirstOrDefaultAsync(mr => mr.MaterialId == materialId && 
                                         mr.RelatedEntityType == "TrainingProgram" &&
                                         mr.RelatedEntityId == trainingProgramId.ToString());
            
            if (relationship == null)
            {
                return false;
            }

            context.MaterialRelationships.Remove(relationship);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed material {MaterialId} from training program {TrainingProgramId}", 
                materialId, trainingProgramId);
            
            return true;
        }

        public async Task<IEnumerable<Material>> GetMaterialsByTrainingProgramAsync(int trainingProgramId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await (from mr in context.MaterialRelationships
                         join m in context.Materials on mr.MaterialId equals m.Id
                         where mr.RelatedEntityType == "TrainingProgram" && 
                               mr.RelatedEntityId == trainingProgramId.ToString()
                         select m).ToListAsync();
        }

        #endregion

        #region Material Dependencies

        public async Task<string> CreateMaterialDependencyAsync(int materialId, int prerequisiteMaterialId, string relationshipType = "prerequisite")
        {
            var relationship = new MaterialRelationship
            {
                MaterialId = materialId,
                RelatedEntityId = prerequisiteMaterialId.ToString(),
                RelatedEntityType = "Material",
                RelationshipType = relationshipType
            };

            var created = await CreateRelationshipAsync(relationship);
            return created.Id;
        }

        public async Task<bool> RemoveMaterialDependencyAsync(int materialId, int prerequisiteMaterialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var relationship = await context.MaterialRelationships
                .FirstOrDefaultAsync(mr => mr.MaterialId == materialId && 
                                         mr.RelatedEntityType == "Material" &&
                                         mr.RelatedEntityId == prerequisiteMaterialId.ToString());
            
            if (relationship == null)
            {
                return false;
            }

            context.MaterialRelationships.Remove(relationship);
            await context.SaveChangesAsync();

            _logger.LogInformation("Removed dependency: Material {MaterialId} no longer requires Material {PrerequisiteId}", 
                materialId, prerequisiteMaterialId);
            
            return true;
        }

        public async Task<IEnumerable<Material>> GetMaterialPrerequisitesAsync(int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await (from mr in context.MaterialRelationships
                         join m in context.Materials on int.Parse(mr.RelatedEntityId) equals m.Id
                         where mr.MaterialId == materialId && 
                               mr.RelatedEntityType == "Material" && 
                               mr.RelationshipType == "prerequisite"
                         select m).ToListAsync();
        }

        public async Task<IEnumerable<Material>> GetMaterialDependentsAsync(int materialId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            return await (from mr in context.MaterialRelationships
                         join m in context.Materials on mr.MaterialId equals m.Id
                         where mr.RelatedEntityType == "Material" && 
                               mr.RelatedEntityId == materialId.ToString() &&
                               mr.RelationshipType == "prerequisite"
                         select m).ToListAsync();
        }

        #endregion
    }
}