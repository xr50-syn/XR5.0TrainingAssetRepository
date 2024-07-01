using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;

    }
}
