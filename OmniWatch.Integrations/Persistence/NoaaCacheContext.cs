using Microsoft.EntityFrameworkCore;
using OmniWatch.Integrations.Contracts.NOA;

namespace OmniWatch.Integrations.Persistence
{
    public class NoaaCacheContext : DbContext
    {
        public DbSet<StormTrack> StormTracks { get; set; }
        public DbSet<StormTrackPointItem> StormPoints { get; set; }
        public DbSet<DbMetadata> Metadata { get; set; }

        public NoaaCacheContext(DbContextOptions<NoaaCacheContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // =========================
            // METADATA
            // =========================
            modelBuilder.Entity<DbMetadata>()
                .HasKey(m => m.Key);

            // =========================
            // STORM TRACK
            // =========================
            modelBuilder.Entity<StormTrack>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<StormTrack>()
                .HasIndex(s => s.Season);

            // =========================
            // STORM POINTS
            // =========================
            modelBuilder.Entity<StormTrackPointItem>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<StormTrackPointItem>()
                .HasIndex(p => p.Time);

            // =========================
            // RELATIONSHIP (EXPLÍCITA)
            // =========================
            modelBuilder.Entity<StormTrackPointItem>()
                .HasOne<StormTrack>()
                .WithMany(s => s.Track)
                .HasForeignKey(p => p.StormTrackId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
        }
    }
}