using ActivityTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ActivityTracker.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Activity> Activities { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Activity>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.ActivityType)
                      .HasMaxLength(50)
                      .HasDefaultValue("Running");

                entity.Property(a => a.RouteGeoJson)
                      .HasColumnType("nvarchar(max)");

                entity.HasOne<ApplicationUser>()
                      .WithMany(u => u.Activities)
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Activity>().HasIndex(a => a.UserId);
            builder.Entity<Activity>().HasIndex(a => a.StartedAt);
        }
    }
}