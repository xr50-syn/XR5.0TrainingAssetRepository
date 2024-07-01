using System.ComponentModel.DataAnnotations;

namespace XR5_0TrainingRepo.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public string? Name { get; set; }
    }
}
