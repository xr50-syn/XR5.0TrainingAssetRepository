using System.ComponentModel.DataAnnotations;

namespace XR50TrainingAssetRepo.Models
{
    public class QueryStore
    {

        public string? Query { get; set; }
        public string? QueryResponse { get; set; }
        [Key]
        public long QueryId { get; set; }

    }
}
