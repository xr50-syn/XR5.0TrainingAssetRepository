using System.ComponentModel.DataAnnotations;

namespace XR5_0TrainingRepo.Models
{
    public class Asset
    {

        public string? Path { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string TrainingId { get; set; }
        public string? ResourceId { get; set; } 

        [Key]
        public long? AssetId { get; set; }
    }
}
