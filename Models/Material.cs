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
        public ChecklistMaterial()
        {
            
            
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
        
        public VideoMaterial()
        {
            Type = Type.Video;
        }
    }

    public class WorkflowMaterial : Material
    {
        // Implementation specific to workflow materials
        public WorkflowMaterial()
        {

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
}
