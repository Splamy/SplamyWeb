using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using RateMapSeveritySaber;

namespace SplamyWeb.Db
{
	public class SplamyContext : DbContext
	{
		public DbSet<RamsesEntry> RamsesEntries { get; set; }
		public DbSet<RamsesMap> RamsesMaps { get; set; }

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
		}
	}
}
