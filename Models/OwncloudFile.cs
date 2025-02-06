using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;


namespace XR5_0TrainingRepo.Models
{
    enum ShareType{
        Group,
        User
    }
    public class Share
    {
        ShareType Type { get; set;}
        string Target {get; set;}
    }
    public class OwncloudFile
    {

        public string? Path { get; set; }
        public string? Description { get; set; }
        public string? OwncloudFileName { get; set; }
        public virtual List<Share>? ShareList { get; set; }

        [Key]
        public string ShareId { get; set; }
        public OwncloudFile()
        {
            ShareId= Guid.NewGuid().ToString();
        }
    }
}
