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
        public string? TenantDirectory {  get; set; }
        public string? OwnerName { get; set;} 
	    public User? Owner {get; set;}
        public virtual List<string>? AdminList { get; set; }
        
        public XR50Tenant(){
    
         }

    }
}
