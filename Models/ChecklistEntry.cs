using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using XR50TrainingAssetRepo.Models;

namespace XR50TrainingAssetRepo.Models
{
    public class ChecklistEntry
    {
        [Key]

        public string ChecklistEntryId { get; set; }

        public string Text { get; set; } = null!;
        public string? Description { get; set; }

        public ChecklistEntry()
        {
            ChecklistEntryId = Guid.NewGuid().ToString();
        }


    }
}
