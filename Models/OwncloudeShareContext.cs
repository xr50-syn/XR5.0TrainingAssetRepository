using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class OwncloudShareContext : DbContext
    {


        public OwncloudShareContext(DbContextOptions<OwncloudShareContext> options)
            : base(options)
        {
        }

        public DbSet<OwncloudShare> OwncloudShare { get; set; } = null!;
    }
}
