using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class WorkflowStep
    {
        [Key]
        
        public string WorkflowStepId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }

        public WorkflowStep()
        {
           WorkflowStepId = Guid.NewGuid().ToString();
        }
    }

}