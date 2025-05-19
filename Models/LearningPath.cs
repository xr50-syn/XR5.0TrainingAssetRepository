using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class LearningPath
    {
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [ForeignKey("TrainingPrograms")]
        public virtual List<string>? TrainingProgramList { get; set; }
        public string TenantName { get; set; }
        [Key]
        public string LearningPathId { get; set; }
        public string Description { get; set; }
        public string LearningPathName { get; set; }    
        public LearningPath()
        {   
            LearningPathId = Guid.NewGuid().ToString();
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();

        }
    }

}