using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class VideoTimestamp
    {
        [Key]
        public int VideoTimestampId { get; set; }

        [ForeignKey("Material")]
        public string VideoTimestampMaterialId { get; set; } = null!;

        public string Title { get; set; } = null!;
        public string Time { get; set; } = null!;
        public string? Description { get; set; }

        [ForeignKey("Materials")]
        public virtual List<string>? MaterialList { get; set; }
    }
}