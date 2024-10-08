using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models {
    public class ResourceManagement
{
        
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string? TrainingId { get; set; }
        public List<Asset>? AssetList { get; }
        [Key]
        public string? ResourceName { get; set; }
        
        public ResourceManagement()
        {
            AssetList = new List<Asset>();
        }
    }
}
