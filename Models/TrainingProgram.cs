using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    
    public class TrainingProgram
    {

        public string? Created_at { get; set; }
        [ForeignKey("Materials")]
        public virtual List<int>? MaterialList { get; set; }
        [ForeignKey("Assets")]
        public virtual List<int>? LearningPathList { get; set;}
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
         
        public TrainingProgram()
        {   
            MaterialList = new List<int>();
            LearningPathList = new List<int>();
        }
    }

}
