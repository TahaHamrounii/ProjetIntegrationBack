using Microsoft.EntityFrameworkCore;
using Message.Models;

namespace Message.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Enable lazy loading
            this.ChangeTracker.LazyLoadingEnabled = false;
            // Don't track changes for read operations
            this.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.HasOne(u => u.Settings)
                      .WithOne(s => s.User)
                      .HasForeignKey<UserSettings>(s => s.UserId);
            });

            // Seed test user
            var userId = "raed123";
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = userId,
                    Username = "raed",
                    Email = "raed@test.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Name = "Raed Test",
                    Avatar = "default-avatar.jpg",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastActiveTime = DateTime.UtcNow
                }
            );

            modelBuilder.Entity<UserSettings>().HasData(
                new UserSettings
                {
                    Id = 1,
                    UserId = userId,
                    IsActive = true,
                    Theme = "dark",
                    Language = "en"
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=ChatApp.db");
            }
        }
    }
}
