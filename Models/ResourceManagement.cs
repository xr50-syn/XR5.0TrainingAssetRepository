using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models {
    public class ResourceManagement
{
        public string? AppName { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string? TrainingName { get; set; }
        public List<Asset>? AssetList { get; }
        
        public string? ResourceName { get; set; }
	[Key]
        public string? ResourceId {get; set;}
        public ResourceManagement()
        {
	    ResourceId= Guid.NewGuid().ToString();
            AssetList = new List<Asset>();
        }
    }
}
