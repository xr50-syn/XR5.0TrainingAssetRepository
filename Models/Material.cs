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
        public string? TenantName { get; set; }
        public string? Description { get; set; }
        public string? MaterialName { get; set; }
        public string? ParentId { get; set; }
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
            TrainingList = new List<string>();
        }
    }
    public enum MaterialType
    {
        Checklist,
        Image,
        Video,
        Workflow,
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
            TrainingList = new List<string>();
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
            TrainingList = new List<string>();
            MaterialType = MaterialType.Image;
        }
    }

    public class VideoMaterial : Material
    {
        // Implementation specific to video materials
        public string? AssetId { get; set; }
        public List<VideoTimestamp> Timestamps { get; set; }
        [ForeignKey("VideoTimestamps")]
        public List<string> TimestapId { get; set; }
        public VideoMaterial()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingList = new List<string>();
            Timestamps = new List<VideoTimestamp>();
            TimestapId= new List<string>();
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
            TrainingList = new List<string>();
            Steps = new List<WorkflowStep>();
            StepIds= new List<string>();
            MaterialType = MaterialType.Workflow;
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
            TrainingList = new List<string>();
            MaterialType = MaterialType.Default;
        }
    }
}
