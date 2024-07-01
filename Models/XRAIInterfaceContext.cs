using Microsoft.EntityFrameworkCore;
using XR5_0TrainingRepo.Models;

namespace XR5_0TrainingRepo.Models
{
    public class XRAIInterfaceContext : DbContext
    {

        public XRAIInterfaceContext(DbContextOptions<XRAIInterfaceContext> options)
           : base(options)
        {
        }

        public DbSet<QueryStore> Queries { get; set; } = null!;
    }

}
