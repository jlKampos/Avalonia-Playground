using Microsoft.EntityFrameworkCore;
using OmniWatch.Integrations.Contracts.NOA;

namespace OmniWatch.Integrations.Persistence
{
    public class NoaaCacheContext : DbContext
    {
        public DbSet<StormTrack> StormTracks { get; set; }
        public DbSet<StormTrackPointItem> StormPoints { get; set; }

        public NoaaCacheContext(DbContextOptions<NoaaCacheContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // O ID do IBTrACS (SID) será a nossa chave primária
            modelBuilder.Entity<StormTrack>().HasKey(s => s.Id);

            modelBuilder.Entity<StormTrackPointItem>().HasKey("Id");

            // Relacionamento: Um StormTrack tem muitos StormPoints
            modelBuilder.Entity<StormTrack>()
                .HasMany(s => s.Track)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexar o tempo para buscas rápidas por ano
            modelBuilder.Entity<StormTrackPointItem>().HasIndex(p => p.Time);
        }
    }
}
