using Microsoft.EntityFrameworkCore;
using NetCoreVideoUpload.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreVideoUpload.Data
{
    public class VideoDbContext : DbContext
    {
        public VideoDbContext (DbContextOptions<VideoDbContext> options)
            : base(options)
        {

        }
        public DbSet<Videos> Videos { get; set; }
    }
}
