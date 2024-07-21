using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class XRAppContext : DbContext
    {
        public XRAppContext(DbContextOptions<XRAppContext> options)
           : base(options)
        {
        }

        public DbSet<XRApp> Apps { get; set; } = null!;
    }
}
