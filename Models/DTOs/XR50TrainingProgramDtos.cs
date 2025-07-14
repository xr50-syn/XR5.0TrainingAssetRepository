using System.ComponentModel.DataAnnotations;

namespace XR50TrainingAssetRepo.Models.DTOs
{
    public class CompleteTrainingProgramRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        // Materials to assign during creation
        public List<int> MaterialIds { get; set; } = new();

        // Learning paths to assign during creation (if needed)
        public List<int> LearningPathIds { get; set; } = new();

        // Optional: Materials with full data (for creation + assignment in one go)
        public List<MaterialCreationRequest>? MaterialsToCreate { get; set; }
    }

    public class MaterialCreationRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string MaterialType { get; set; } = "Default"; // Video, Image, Checklist, etc.

        // Type-specific properties (only set what's needed based on MaterialType)
        public string? AssetId { get; set; }
        public string? VideoPath { get; set; }
        public int? VideoDuration { get; set; }
        public string? VideoResolution { get; set; }
        public string? ImagePath { get; set; }
        public string? ChatbotConfig { get; set; }
        public string? MessageType { get; set; }
        public string? MessageText { get; set; }
    }

    public class CompleteTrainingProgramResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Created_at { get; set; }

        // Complete material information
        public List<MaterialResponse> Materials { get; set; } = new();

        // Complete learning path information
        public List<LearningPathResponse> LearningPaths { get; set; } = new();

        // Summary information
        public TrainingProgramSummary Summary { get; set; } = new();
    }

    public class MaterialResponse
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string Type { get; set; } = "";
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        // Asset information (if applicable)
        public string? AssetId { get; set; }

        // Type-specific properties (populated based on material type)
        public Dictionary<string, object?> TypeSpecificProperties { get; set; } = new();

        // Assignment metadata (if from complex relationships)
        public AssignmentMetadata? Assignment { get; set; }
    }

    public class LearningPathResponse
    {
        public int Id { get; set; }
        public string LearningPathName { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class AssignmentMetadata
    {
        public string AssignmentType { get; set; } = "Simple"; // Simple or Complex
        public string? RelationshipType { get; set; }
        public int? DisplayOrder { get; set; }
        public int? RelationshipId { get; set; } // For complex relationships
    }

    public class TrainingProgramSummary
    {
        public int TotalMaterials { get; set; }
        public int TotalLearningPaths { get; set; }
        public Dictionary<string, int> MaterialsByType { get; set; } = new();
        public DateTime? LastUpdated { get; set; }
    }
}