using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;
namespace XR5_0TrainingRepo.Models
{
    public class XRAppManagement
    {
        [Key]
        public long AppId { get; set; }
        public string? AppName { get; set; }
        public XRAppManagement() { }

    }
}
