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

        public DbSet<XR50App> Apps { get; set; } = null!;
	public DbSet<QueryStore> Queries { get; set; } = null!;
	public DbSet<User> Users { get; set; } = null!;
	public DbSet<TrainingModule> Trainings { get; set; } = null!;
	public DbSet<ResourceManagement> Resource { get; set; } = null!;
	public DbSet<OwncloudShare> OwncloudShare { get; set; } = null!;
	public DbSet<Asset> Asset { get; set; } = null!;

    }
}
