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
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
         [ForeignKey("TrainingPrograms")]
        public virtual List<string>? TrainingProgramList { get; set; }
        [Key]
        public string? MaterialId { get; set; }
        [Required]
        public MaterialType MaterialType { get; set; }

        public Material()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
        }
    }
    public enum MaterialType
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
        public List<ChecklistEntry> Entries { get; set; }
        [ForeignKey("ChecklistEntries")]
        public List<int> EntryId { get; set; }
        public ChecklistMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            Entries = new List<ChecklistEntry>();
            EntryId = new List<int>();
            MaterialType = MaterialType.Checklist;
        }
        
    }

    public class ImageMaterial : Material
    
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public ImageMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.Image;
        }
    }

    public class VideoMaterial : Material
    {
        // Implementation specific to video materials
        public string? AssetId { get; set; }
        public List<VideoTimestamp> Timestamps { get; set; }
        [ForeignKey("VideoTimestamps")]
        public List<string> TimestampId { get; set; }
        public VideoMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            Timestamps = new List<VideoTimestamp>();
            TimestampId= new List<string>();
            MaterialType = MaterialType.Video;
        }
    }

    public class WorkflowMaterial : Material
    {
        // Implementation specific to workflow materials
        public List<WorkflowStep> Steps { get; set; }
        [ForeignKey("WorkflowSteps")]
        public List<string> StepIds { get; set; }
        public WorkflowMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            Steps = new List<WorkflowStep>();
            StepIds= new List<string>();
            MaterialType = MaterialType.Workflow;
        }
    }
    public class MQTT_TemplateMaterial : Material
    {
        // Implementation specific to image materials
        public string? message_type { get; set; }
        public string? message_text { get; set; }
        public MQTT_TemplateMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.Default;
        }
    }
     public class PDFMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public PDFMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.PDF;
        }
    }
     public class UnityDemoMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public UnityDemoMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.UnityDemo;
        }
    }
    public class ChatbotMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public ChatbotMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.Chatbot;
        }
    }
    public class QuestionnaireMaterial : Material
    {
        // Implementation specific to image materials
        public List<QuestionnaireEntry> Entries { get; set; }
        public QuestionnaireMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            Entries = new List<QuestionnaireEntry>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.Questionnaire;
        }
    }
    public class DefaultMaterial : Material
    {
        // Implementation specific to image materials
        public string? AssetId { get; set; }
        public DefaultMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();
            MaterialType = MaterialType.Default;
        }
    }
}
