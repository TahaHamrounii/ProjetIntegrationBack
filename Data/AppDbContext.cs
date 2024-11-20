using Message.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Message.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
        public DbSet<Mssg> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite(@"Data Source=c:\Temp\Demo.db");
    }

}
