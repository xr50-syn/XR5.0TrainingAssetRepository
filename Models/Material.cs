using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class Material
    {
        public string? TennantName { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string? TrainingName { get; set; }
        public string? ParentId { get; set; }
        public string? MaterialName { get; set; }
        [ForeignKey("Assets")]
        public virtual List<string>? AssetList { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [Key]
        public string? MaterialId { get; set; }
        [Required]
        public MaterialType MaterialType { get; set; }

        public Material()
        {
            MaterialId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            AssetList = new List<string>();
        }
    }
    public enum MaterialType
    {
        Checklist,
        Image,
        Video,
        Workflow
    }
}
