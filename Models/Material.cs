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
        // Implementation specific to checklist materials
        public List<ChecklistEntry> ChecklistEntries { get; set; }
        public ChecklistMaterial()
        {
            ChecklistEntries = new List<ChecklistEntry>();
            Type = Type.Checklist;
        }

    }

    public class ImageMaterial : Material

    {
        // Implementation specific to image materials
        public ImageMaterial()
        {
            Type = Type.Image;
        }
    }

    public class VideoMaterial : Material
    {
        // Implementation specific to video materials
        public List<VideoTimestamp> VideoTimestamps { get; set; }
        public VideoMaterial()
        {
            VideoTimestamps = new List<VideoTimestamp>();
            Type = Type.Video;
        }
    }

    public class WorkflowMaterial : Material
    {
        // Implementation specific to workflow materials
        public List<WorkflowStep> WorkflowSteps { get; set; }
        public WorkflowMaterial()
        {
            WorkflowSteps = new List<WorkflowStep>();
            Type = Type.Workflow;
        }
    }
    public class MQTT_TemplateMaterial : Material
    {
        // Implementation specific to image materials
        public string? message_type { get; set; }
        public string? message_text { get; set; }
        public MQTT_TemplateMaterial()
        {

            Type = Type.MQTT_Template;
        }
    }
    public class PDFMaterial : Material
    {
        // Implementation specific to image materials
        public PDFMaterial()
        {
            Type = Type.PDF;
        }
    }
    public class UnityDemoMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public UnityDemoMaterial()
        {
            Type = Type.UnityDemo;
        }
    }
    public class ChatbotMaterial : Material
    {
        // Implementation specific to image materials

        public ChatbotMaterial()
        {
            Type = Type.Chatbot;
        }
    }
    public class QuestionnaireMaterial : Material
    {
        // Implementation specific to image materials
        public QuestionnaireMaterial()
        {

            Type = Type.Questionnaire;
        }
    }
    public class DefaultMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public DefaultMaterial()
        {

            Type = Type.Default;
        }
    }
    public class MaterialRelationship
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        public int MaterialId { get; set; }
        public string RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; }  // "Asset", "Material", etc.
        public string? RelationshipType { get; set; }  // "Contains", "References", etc.
        public int? DisplayOrder { get; set; }
        
        // Navigation property to Material
        public virtual Material Material { get; set; }
    }
}
