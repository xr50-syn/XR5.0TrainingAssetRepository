using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Eventing.Reader;

namespace XR5_0TrainingRepo.Models
{
    public class User
    {
        public string TennantName;
        public string? FullName { get; set; }
        public string? UserEmail { get; set; }
        public string? Password { get; set; }
        public bool admin { get; set; }
        [Key]
        public string? UserName { get; set; }

    }
    
    public class Group {
        public virtual List<string>? GroupList { get; set; }
         [Key]
         public string? GroupName { get; set; }

    }
}
