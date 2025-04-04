using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class VideoTimestamp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VideoTimestampId { get; set; }

        
        public string Title { get; set; } = null!;
        public string Time { get; set; } = null!;
        public string? Description { get; set; }

        public VideoTimestamp()
        {
           VideoTimestampId = Guid.NewGuid().ToString();
        }
    }
}