using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
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