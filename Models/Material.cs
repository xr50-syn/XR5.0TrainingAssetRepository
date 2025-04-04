using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Controllers;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class Material
    {
        public string? TennantName { get; set; }
        public string? Description { get; set; }
        public string? MaterialName { get; set; }
        public string? ParentId { get; set; }
        [ForeignKey("Assets")]
        public virtual List<string>? AssetList { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
         [ForeignKey("Trainings")]
        public virtual List<string>? TrainingList { get; set; }
        [Key]
        public string? MaterialId { get; set; }
        [Required]
        public MaterialType MaterialType { get; set; }

        public Material()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
            TrainingList = new List<string>();
        }
    }
    public enum MaterialType
    {
        Checklist,
        Image,
        Video,
        Workflow
    }


    public class ChecklistMaterial : Material
    {
        // Implementation specific to checklist materials
        public List<string> Entries { get; set; }
        
        public ChecklistMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
            TrainingList = new List<string>();
            Entries = new List<string>();
            MaterialType = MaterialType.Checklist;
        }
        
    }

    public class ImageMaterial : Material
    
    {
        // Implementation specific to image materials
        public ImageMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
            TrainingList = new List<string>();
            MaterialType = MaterialType.Image;
        }
    }

    public class VideoMaterial : Material
    {
        // Implementation specific to video materials
        public List<string> Timestamps { get; set; }
        public VideoMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
            TrainingList = new List<string>();
            Timestamps = new List<string>();
            MaterialType = MaterialType.Video;
        }
    }

    public class WorkflowMaterial : Material
    {
        // Implementation specific to workflow materials
        public List<WorkflowStep> Steps { get; set; }
        public WorkflowMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
            TrainingList = new List<string>();
            Steps = new List<WorkflowStep>();
            MaterialType = MaterialType.Workflow;
        }
    }
}
