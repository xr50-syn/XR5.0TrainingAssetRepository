using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Eventing.Reader;

namespace XR50TrainingAssetRepo.Models
{
    public class User
    {
        public string TenantName { get; set; }
        public string? FullName { get; set; }
        public string? UserEmail { get; set; }
        public string? Password { get; set; }
        public bool admin { get; set; }
        [Key]
        public string? UserName { get; set; }

    }
    
    public class Group {
        [ForeignKey("Users")]
        public virtual List<string>? UserList { get; set; }
        public string? TenantName {get; set;}
         [Key]
         public string? GroupName { get; set; }

    }
}
