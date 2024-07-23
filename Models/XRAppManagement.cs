using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;
namespace XR5_0TrainingRepo.Models
{
    public class XR50App
    {
        [Key]
        public string AppName { get; set; }
        public string? OwncloudGroup { get; set; }
        public string? Description { get; set; }
        public XR50App() { }

    }
}
