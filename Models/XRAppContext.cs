using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class XR50AppContext : DbContext
    {
        public XR50AppContext(DbContextOptions<XR50AppContext> options)
           : base(options)
        {
        }

        public DbSet<XR50App> Apps { get; set; } = null!;
      
    }
}
