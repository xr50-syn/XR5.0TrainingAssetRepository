using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
     public enum ShareType{
        Group,
        User
    }
    public class Share
    {
        [Key]
        public string ShareId { get; set; }
        public string? TenantName { get; set; }
        public string? FileId { get; set; }
        public ShareType Type { get; set;}
        public string Target {get; set;}
        public Share()
        {
            ShareId= Guid.NewGuid().ToString();
        }
    }
    public class OwncloudDirectory {
        public string? TenantName {get;set;}
        [Key]
        public string? OwncloudPath {get;set;}
        public OwncloudDirectory() {

        }
    }


    public class Asset
    {
        public string? TenantName { get; set; }
        public string? Description { get; set; }
        [ForeignKey("Shares")]
        public virtual List<string>? ShareList { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; } 
        public string? Type { get; set; }
	    [Key]
        public string? OwncloudFileName { get; set; }
        public Asset ()
        {
            OwncloudFileName = Guid.NewGuid().ToString();
            ShareList = new List<string>();
            MaterialList= new List<string>();
            
        }
    }
}
