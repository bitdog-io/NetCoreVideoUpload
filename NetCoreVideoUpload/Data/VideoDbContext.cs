using Microsoft.EntityFrameworkCore;
using NetCoreVideoUpload.Models;

namespace NetCoreVideoUpload.Data
{
    public class VideoDbContext : DbContext
    {
        public VideoDbContext(DbContextOptions<VideoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Videos> Videos { get; set; }
    }
}