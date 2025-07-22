// Updated Material.cs with all properties

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Controllers;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class Material
    {
        public string? Description { get; set; }
        public string? Name { get; set; }
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }

        [Key]
        public int Id { get; set; }
        [Required]
        public Type Type { get; set; }
        public virtual ICollection<ProgramMaterial> ProgramMaterials { get; set; } = new List<ProgramMaterial>();
        public virtual ICollection<MaterialRelationship> MaterialRelationships { get; set; } = new List<MaterialRelationship>();
        public Material()
        {

        }
    }

    public enum Type
    {
        Image,
        Video,
        PDF,
        UnityDemo,
        Chatbot,
        Questionnaire,
        Checklist,
        Workflow,
        MQTT_Template,
        Answers,
        Default
    }

    public class ChecklistMaterial : Material
    {
        // Uses separate ChecklistEntries table (no new columns in Materials table)
        public List<ChecklistEntry> ChecklistEntries { get; set; }
        
        public ChecklistMaterial()
        {
            ChecklistEntries = new List<ChecklistEntry>();
            Type = Type.Checklist;
        }
    }

    public class ImageMaterial : Material
    {
        // Image-specific properties stored in Materials table
        public int? AssetId { get; set; }
        public string? ImagePath { get; set; }
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public string? ImageFormat { get; set; }
        
        public ImageMaterial()
        {
            Type = Type.Image;
        }
    }

    public class VideoMaterial : Material
    {
        // Video-specific properties stored in Materials table
        public int? AssetId { get; set; }
        public string? VideoPath { get; set; }
        public int? VideoDuration { get; set; }  // Duration in seconds
        public string? VideoResolution { get; set; }  // e.g., "1920x1080"
        
        // Uses separate VideoTimestamps table
        public List<VideoTimestamp> VideoTimestamps { get; set; }
        
        public VideoMaterial()
        {
            VideoTimestamps = new List<VideoTimestamp>();
            Type = Type.Video;
        }
    }

    public class WorkflowMaterial : Material
    {
        // Uses separate WorkflowSteps table (no new columns in Materials table)
        public List<WorkflowStep> WorkflowSteps { get; set; }
        
        public WorkflowMaterial()
        {
            WorkflowSteps = new List<WorkflowStep>();
            Type = Type.Workflow;
        }
    }

    public class MQTT_TemplateMaterial : Material
    {
        // MQTT-specific properties stored in Materials table
        public string? message_type { get; set; }
        public string? message_text { get; set; }
        
        public MQTT_TemplateMaterial()
        {
            Type = Type.MQTT_Template;
        }
    }

    public class PDFMaterial : Material
    {
        // PDF-specific properties stored in Materials table
        public int? AssetId { get; set; }
        public string? PdfPath { get; set; }
        public int? PdfPageCount { get; set; }
        public long? PdfFileSize { get; set; }  // File size in bytes
        
        public PDFMaterial()
        {
            Type = Type.PDF;
        }
    }

    public class UnityDemoMaterial : Material
    {
        // Unity-specific properties stored in Materials table
        public int? AssetId { get; set; }
        public string? UnityVersion { get; set; }
        public string? UnityBuildTarget { get; set; }  // e.g., "WebGL", "Windows", "Android"
        public string? UnitySceneName { get; set; }
        
        public UnityDemoMaterial()
        {
            Type = Type.UnityDemo;
        }
    }

    public class ChatbotMaterial : Material
    {
        // Chatbot-specific properties stored in Materials table
        public string? ChatbotConfig { get; set; }  // JSON configuration
        public string? ChatbotModel { get; set; }   // e.g., "gpt-4", "claude-3"
        public string? ChatbotPrompt { get; set; }  // System prompt
        
        public ChatbotMaterial()
        {
            Type = Type.Chatbot;
        }
    }

    public class QuestionnaireMaterial : Material
    {
        // Questionnaire-specific properties stored in Materials table
        public string? QuestionnaireConfig { get; set; }  // JSON configuration
        public string? QuestionnaireType { get; set; }    // e.g., "multiple_choice", "essay", "mixed"
        public decimal? PassingScore { get; set; }        // Percentage needed to pass
        
        // Could also use separate QuestionnaireEntries table if needed
        public List<QuestionnaireEntry> QuestionnaireEntries { get; set; }
        
        public QuestionnaireMaterial()
        {
            QuestionnaireEntries = new List<QuestionnaireEntry>();
            Type = Type.Questionnaire;
        }
    }

    public class DefaultMaterial : Material
    {
        // Generic material with asset support
        public int? AssetId { get; set; }
        
        public DefaultMaterial()
        {
            Type = Type.Default;
        }
    }

    public class MaterialRelationship
    {
        [Key]
        public int Id { get; set; }
        
        public int MaterialId { get; set; }
        public string RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; }  // "Asset", "Material", etc.
        public string? RelationshipType { get; set; }  // "Contains", "References", etc.
        public int? DisplayOrder { get; set; }
        
        // Navigation property to Material
        public virtual Material Material { get; set; }
    }
}