using System.ComponentModel.DataAnnotations;
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
        public ShareType Type { get; set;}
        public string Target {get; set;}
        public Share()
        {
            ShareId= Guid.NewGuid().ToString();
        }
    }
    public class OwncloudDirectory {
        public string? TennantName {get;set;}
        [Key]
        public string? OwncloudPath {get;set;}
        public OwncloudDirectory() {

        }
    }
    public class OwncloudFile
    {
        
        public string TennantName;
        public string? OwncloudPath { get; set; }
        public string? Description { get; set; }
        public string? Type {get;set;}
        public virtual List<Share>? ShareList { get; set; }
        [Key]
        public string OwncloudFileName { get; set; }
        public OwncloudFile()
        {
            OwncloudFileName= Guid.NewGuid().ToString();
        }
    }
}
