using System.ComponentModel.DataAnnotations;

namespace XR50TrainingAssetRepo.Models.DTOs
{
    public class AssetReferenceData
    {
        public string? Filename { get; set; }
        public string? Description { get; set; }
        public string? Filetype { get; set; }
        public string? Src { get; set; }
        public string? URL { get; set; }
    }
}
