using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class XR50Tenant
    {
        [Key]
        public string TenantName { get; set; }
        public string? TenantGroup { get; set; }
        public string? TenantSchema { get; set; }
        public string? Description { get; set; }
        public string? TenantDirectory { get; set; }
        public string? OwnerName { get; set; }
        public User? Owner { get; set; }
        public virtual ICollection<TenantAdmin> TenantAdmins { get; set; } = new List<TenantAdmin>();

        public XR50Tenant()
        {

        }
    }
     public class TenantAdmin
    {
        public string TenantName { get; set; }
        public string UserName { get; set; }
            
        // Navigation properties  
        public virtual XR50Tenant Tenant { get; set; }
        public virtual User User { get; set; }
    }
}
