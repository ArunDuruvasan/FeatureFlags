using FeatureFlagAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagAPI
{
    public class AppDbContext: DbContext
    {
        public DbSet<FeatureFlag> FeatureFlags { get; set; }
        public DbSet<FeatureOverride> FeatureOverrides { get; set; }
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FeatureFlag constraints
            modelBuilder.Entity<FeatureFlag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            // FeatureOverride constraints
            modelBuilder.Entity<FeatureOverride>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.FeatureId, e.OverrideType, e.OverrideKey }).IsUnique();
                entity.Property(e => e.OverrideType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.OverrideKey).IsRequired().HasMaxLength(100);

                // Foreign key relationship
                entity.HasOne<FeatureFlag>()
                    .WithMany()
                    .HasForeignKey(e => e.FeatureId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
