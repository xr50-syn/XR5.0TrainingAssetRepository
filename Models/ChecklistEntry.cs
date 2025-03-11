using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class ChecklistEntry
    {
        [Key]
        public int ChecklistEntryId { get; set; }

        [ForeignKey("Material")]
        public string ChecklistMaterialId { get; set; } = null!;

        public string Text { get; set; } = null!;
        public string? Description { get; set; }

        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }

    }
}
