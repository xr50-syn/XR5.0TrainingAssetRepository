using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{

    public class OwncloudShare
    {

        public string? Path { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public string? TrainingId { get; set; }
        public string? ResourceId { get; set; }
        public string? AppId { get; set; }
        public string? AssetId { get; set; }
        public string? Type { get; set; }

        [Key]
        public string ShareId { get; set; }
        public OwncloudShare()
        {
            ShareId= Guid.NewGuid().ToString();
        }
    }
}
