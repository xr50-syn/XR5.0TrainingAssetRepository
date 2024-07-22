using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace XR5_0TrainingRepo.Models
{
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? UserEmail { get; set; }
        public string? Password { get; set; }

        public long AppId { get; set; }

    }

}
