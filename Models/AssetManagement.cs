using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    [PrimaryKey(nameof(AppName), nameof(TrainingName), nameof(ResourceName), nameof(OwncloudFileName))]
    public class Asset
    {

        public string? Path { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string? OwncloudPath { get; set; }
        public string? AppName { get; set; }
        public string? TrainingName { get; set; }
        public string? ResourceName { get; set; } 
        public string? Type { get; set; }

        public string? AssetId { get; set; }
        public Asset ()
        {
            AssetId = Guid.NewGuid().ToString();
            
        }
    }
}
