using Microsoft.EntityFrameworkCore;
using Mono.TextTemplating;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    [PrimaryKey(nameof(AppName), nameof(TrainingName))]
    public class TrainingModule
    {

        
        public string? UseCase { get; set; }
        public List<ResourceManagement>? ResourceList { get; set; }

        public string? AppName { get; set; }
        
        public string? TrainingName { get; set; }

        public TrainingModule()
        {
            ResourceList = new List<ResourceManagement>();

        }
    }

}
