using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace XR5_0TrainingRepo.Models
{
  
      public class ResourceContext : DbContext
    {
        public ResourceContext(DbContextOptions<ResourceContext> options)
        : base(options)
        {
        }

        public DbSet<ResourceManagement> Resource { get; set; } = null!;
    }

}

