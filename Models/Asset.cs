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
        public string? FileId { get; set; }
        public ShareType Type { get; set;}
        public string Target {get; set;}
        public Share()
        {
            ShareId= Guid.NewGuid().ToString();
        }
    }
    public class TenantDirectory {
        public string? TenantName {get;set;}
        [Key]
        public string? TenantPath {get;set;}
        public TenantDirectory() {

        }
    }


    public class Asset
    {
        public string? Description { get; set; }
        public string? Src { get; set; }
        public string? Filetype { get; set; }
        public string Filename  { get; set; }
        public string URL { get; set; }
	    [Key]
        public int Id { get; set; }
        public Asset ()
        {
            
        }
    }
}
