using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    [PrimaryKey(nameof(TenantName), nameof(TrainingName))]
    public class TrainingModule
    {

        public string? UseCase { get; set; }
        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
        [ForeignKey("Assets")]
        public virtual List<string>? AssetList { get; set; }
        public string TenantName { get; set; }
        public string TrainingName { get; set; }
         
        public TrainingModule()
        {   
            MaterialList = new List<string>();
            AssetList = new List<string>();

        }
    }

}
