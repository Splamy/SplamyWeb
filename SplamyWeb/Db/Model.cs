using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;

namespace SplamyWeb.Db
{
	public class SplamyContext : DbContext
	{
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		public DbSet<RamsesEntry> RamsesEntries { get; set; }
		//public DbSet<RamsesMap> RamsesMaps { get; set; }
		public DbSet<StoreEntry> StoreTable { get; set; }
		public DbSet<TabStatsEntryDto> TabStatsTable { get; set; }

		public SplamyContext(DbContextOptions options) : base(options)
		{
			//Log.Info("Created context");
		}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			=> optionsBuilder
			.UseNpgsql("Host=localhost;Database=splamy_db;Username=postgres;Password=postgres")
			.EnableSensitiveDataLogging();

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RamsesMap>()
				.HasKey(x => new { x.RamsesId, x.Characteristic, x.IndexDifficulty });

			modelBuilder.Entity<RamsesMap>()
				.HasOne(x => x.RamsesEntry)
				.WithMany(x => x.Maps)
				.HasForeignKey(e => e.RamsesId);

			modelBuilder.Entity<TabStatsFactoryDto>()
				.HasKey(x => new { x.TabStatsId, x.FactoryName });

			modelBuilder.Entity<TabStatsFactoryDto>()
				.HasOne(x => x.TabStatsEntry)
				.WithMany(x => x.SongStats)
				.HasForeignKey(e => e.TabStatsId);

			modelBuilder.Entity<CachedDayStats>().HasNoKey();
		}
	}
}
