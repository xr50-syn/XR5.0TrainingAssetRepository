using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    [PrimaryKey(nameof(TennantName), nameof(TrainingName))]
    public class TrainingModule
    {

        
        public string? UseCase { get; set; }
        public string TrainingId { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [ForeignKey("Assets")]
        public virtual List<string>? AssetList { get; set; }
        public string TennantName { get; set; }
        public string TrainingName { get; set; }
         
        public TrainingModule()
        {
            TrainingId= Guid.NewGuid().ToString();;	    
            MaterialList = new List<string>();
            AssetList = new List<string>();

        }
    }

}
