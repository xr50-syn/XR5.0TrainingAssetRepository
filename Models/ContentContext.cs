using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class ContentContext : DbContext
    {
        public ContentContext(DbContextOptions<ContentContext> options)
        : base(options)
        {
        }

        public DbSet<Content> Content { get; set; } = null!;
    }
}

