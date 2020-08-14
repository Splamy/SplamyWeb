using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SplamyWeb.Db
{
	public class SplamyContext : DbContext
	{
		public DbSet<RamsesEntry> RamsesEntries { get; set; }
		public DbSet<RamsesMap> RamsesMaps { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			=> optionsBuilder.UseNpgsql("Host=localhost;Database=splamy_db;Username=postgres;Password=postgres");

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<RamsesMap>()
				.HasKey(x => new { x.RamsesId, x.Characteristic, x.Difficulty });

			modelBuilder.Entity<RamsesMap>()
				.HasOne(x => x.RamsesEntry)
				.WithMany(x => x.Maps)
				.HasForeignKey(e => e.RamsesId);
		}
	}

	[Table("ramses_entry")]
	public class RamsesEntry
	{
		/// <summary>Hexadecimal beatsaver map id.</summary>
		[Key]
		[JsonIgnore]
		public long Id { get; set; }
		/// <summary>Version of the ramses engine this result was generated with.</summary>
		[JsonPropertyName("ramsesVersion")]
		public string Version { get; set; }
		[JsonPropertyName("maps")]
		public List<RamsesMap> Maps { get; set; }
	}

	[Table("ramses_map")]
	public class RamsesMap
	{
		public long RamsesId { get; set; }
		public RamsesEntry RamsesEntry { get; set; }

		// Notes:
		// - Characteristic+Difficulty should be unique
		// - Beatmap name should be unique
		// 

		/// <summary>Internal mode name (Standard, 90°, 360°,...)</summary>
		[JsonPropertyName("characteristic")]
		public string Characteristic { get; set; }
		/// <summary>Internal difficulty name</summary>
		[JsonPropertyName("difficulty")]
		public byte Difficulty { get; set; }

		[JsonPropertyName("maxDifficulty")]
		public float MaxDifficulty { get; set; }
		[JsonPropertyName("avgDifficulty")]
		public float AvgDifficulty { get; set; }
		[JsonPropertyName("graph")]
		public float[] Graph { get; set; }
	}
}
