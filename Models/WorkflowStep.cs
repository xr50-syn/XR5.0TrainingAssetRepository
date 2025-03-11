using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class WorkflowStep
    {
        [Key]
        public int WorkflowStepId { get; set; }

        [ForeignKey("Material")]
        public string WorkflowMaterialId { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string? Content { get; set; }

        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
    }

}