using Microsoft.EntityFrameworkCore;
using Message.Models;

namespace Message.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Messages> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define primary key using Fluent API for Messages table
            modelBuilder.Entity<Messages>()
                        .HasKey(m => m.MessageId);  // Define the primary key

            base.OnModelCreating(modelBuilder);
        }
    }
}
