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
                      .HasForeignKey<UserSettings>(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                
                entity.Property(e => e.Theme).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Language).HasMaxLength(10).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
            });

            // Seed test user
            var userId = "raed123";
            var now = DateTime.UtcNow;
            
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = userId,
                    Username = "raed",
                    Email = "raed@test.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Name = "Raed Test",
                    Avatar = "default-avatar.jpg",
                    CreatedAt = now,
                    UpdatedAt = now,
                    LastActiveTime = now
                }
            );

            modelBuilder.Entity<UserSettings>().HasData(
                new UserSettings
                {
                    Id = 1,
                    UserId = userId,
                    IsActive = true,
                    Theme = "dark",
                    Language = "en",
                    CreatedAt = now,
                    UpdatedAt = now
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

        public override int SaveChanges()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is UserSettings && e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                ((UserSettings)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is UserSettings && e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                ((UserSettings)entityEntry.Entity).UpdatedAt = DateTime.UtcNow;
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
