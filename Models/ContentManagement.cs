using System.ComponentModel.DataAnnotations;

namespace XR5_0TrainingRepo.Models
{
    public class Content
    {

        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? FileName { get; set; }
        public long? TrainingId { get; set; }
        [Key]
        public long ContentId { get; set; }
    }
}
