using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models {
    public class ResourceManagement
{
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? FileName { get; set; }
        public long? TrainingId { get; set; }
        public long? ContextId { get; set; }
        [Key]
        public long ResourceId { get; set; }
    }
}
