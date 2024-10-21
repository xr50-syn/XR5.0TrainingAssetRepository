using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;
namespace XR5_0TrainingRepo.Models
{
    public class XR50App
    { 
        
        public string? OwncloudGroup { get; set; }
        public string? Description { get; set; }
        public string? OwncloudDirectory {  get; set; }
        public User AdminUser { get; set; }
       
        public List<TrainingModule>? TrainingList { get; set; }

        [Key]
        public string AppName { get; set; }
        
        public XR50App() 
        {
          
            TrainingList = new List<TrainingModule>();
        }

    }
}
