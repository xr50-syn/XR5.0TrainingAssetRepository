using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class LearningPath
    {
        [Key]
        public int Id { get; set; }
        public string Description { get; set; }
        public string LearningPathName { get; set; }    
        public LearningPath()
        {  

        }
    }

}