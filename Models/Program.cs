using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    [PrimaryKey(nameof(TenantName), nameof(ProgramName ))]
    public class TrainingProgram
    {

        public string? UseCase { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [ForeignKey("Assets")]
        public virtual List<string>? AssetList { get; set; }
        [ForeignKey("LearningPaths")]
        public virtual List<string>? LearningPathList { get; set;}
        public string TenantName { get; set; }
        public string ProgramName  { get; set; }
         
        public TrainingProgram()
        {   
            MaterialList = new List<string>();
            AssetList = new List<string>();
            LearningPathList = new List<string>();
        }
    }

}
