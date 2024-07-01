using System.ComponentModel.DataAnnotations;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class TrainingActivity
    {

        public string? Name { get; set; }
        public string? Pilot { get; set; }
        public string? UseCase { get; set; }
        public  List<Content> ContentList{ get; }


        [Key]
        public long TrainingId { get; set; }

    }

}
