using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class AssetContext : DbContext
    {
        public AssetContext(DbContextOptions<AssetContext> options)
        : base(options)
        {
        }

        public DbSet<Asset> Asset { get; set; } = null!;
    }
}

