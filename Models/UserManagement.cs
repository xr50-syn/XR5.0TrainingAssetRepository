using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Eventing.Reader;

namespace XR50TrainingAssetRepo.Models
{
    public class User
    {
        public string? FullName { get; set; }
        public string? UserEmail { get; set; }
        public string? Password { get; set; }
        public bool admin { get; set; }
        [Key]
        public string? UserName { get; set; }

    }

    public class Group
    {
        public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
        public string? TenantName { get; set; }
        [Key]
        public string? GroupName { get; set; }
    }
    public class GroupUser
    {
        public string GroupName { get; set; }
        public string UserName { get; set; }
        
        // Navigation properties
        public virtual Group Group { get; set; }
        public virtual User User { get; set; }
    }

}
