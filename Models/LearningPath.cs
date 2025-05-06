using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    [PrimaryKey(nameof(TenantName), nameof(TrainingProgramName))]
    public class LearningPath
    {

        public string? UseCase { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [ForeignKey("TrainingPrograms")]
        public virtual List<string>? TrainingProgramList { get; set; }
        public string TenantName { get; set; }
        public string TrainingProgramName { get; set; }
         
        public LearningPath()
        {   
            MaterialList = new List<string>();
            TrainingProgramList = new List<string>();

        }
    }

}