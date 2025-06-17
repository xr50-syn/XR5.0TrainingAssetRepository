using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class VideoTimestamp
    {
        [Key]

        public int id { get; set; }
        public string Title { get; set; } = null!;
        public string Time { get; set; } = null!;
        public string? Description { get; set; }

        public VideoTimestamp()
        {
        
        }
    }
}