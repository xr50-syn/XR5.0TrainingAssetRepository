using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class XR50RepoContext : DbContext
    {
        public XR50RepoContext(DbContextOptions<XR50RepoContext> options)
           : base(options)
        {
        }

        public DbSet<XR50Tennant> Tennants { get; set; } = null!;
	    public DbSet<User> Users { get; set; } = null!;
	    public DbSet<TrainingModule> Trainings { get; set; } = null!;
	    public DbSet<Material> Materials { get; set; } = null!;
	    public DbSet<OwncloudFile> OwncloudFiles{ get; set; } = null!;
	    public DbSet<Asset> Assets { get; set; } = null!;

    }
}
