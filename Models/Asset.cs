using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
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
        public string? FileName { get; set; }
        public Asset ()
        {
            FileName = Guid.NewGuid().ToString();
            ShareList = new List<string>();
            MaterialList= new List<string>();
            
        }
    }
}
