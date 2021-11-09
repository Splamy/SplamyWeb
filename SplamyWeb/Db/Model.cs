using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SplamyWeb.Components;

namespace SplamyWeb.Db;

public class SplamyContext : DbContext
{
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
	public DbSet<RamsesSongDto> RamsesSongs { get; set; }
	public DbSet<StoreEntry> StoreTable { get; set; }
	public DbSet<TabStatsPingDto> TabStatsPings { get; set; }
	public DbSet<TabStatsFactoryDto> TabStatsFactories { get; set; }
	public DbSet<LoginData> User { get; set; }
	public DbSet<NightlyProject> NightlyProjects { get; set; }
	public DbSet<NightlyBranch> NightlyBranches { get; set; }
	public DbSet<NightlyBuild> NightlyBuilds { get; set; }
	public DbSet<LanguageEntry> LanguageEntries { get; set; }

	// https://docs.microsoft.com/en-us/ef/core/miscellaneous/logging?tabs=v3
	public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });
	private readonly string? connectionString;

	public SplamyContext(DbContextOptions options, IConfiguration conf) : base(options)
	{
		connectionString = conf.GetConnectionString("DefaultConnection");
		//Log.Info("Created context");
	}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		=> optionsBuilder
#if DEBUG
		//.UseLoggerFactory(MyLoggerFactory)
		.EnableSensitiveDataLogging()
#endif
			.UseNpgsql(connectionString)
		;

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Ramses ***

		modelBuilder.Entity<RamsesMapDto>()
			.HasKey(x => new { x.RamsesId, x.Characteristic, x.IndexDifficulty });
		modelBuilder.Entity<RamsesMapDto>()
			.HasOne(map => map.RamsesEntry)
			.WithMany(entry => entry.Maps)
			.HasForeignKey(map => map.RamsesId)
			.HasPrincipalKey(entry => entry.Id);

		// *** Tab Stats

		//modelBuilder.Entity<TabStatsPingDto>();

		modelBuilder.Entity<TabStatsFactoryDto>()
			.HasKey(x => new { x.TabStatsId, x.FactoryName });
		modelBuilder.Entity<TabStatsFactoryDto>()
			.HasOne(factory => factory.TabStatsEntry)
			.WithMany(entry => entry.SongStats)
			.HasForeignKey(factory => factory.TabStatsId);

		// This tells EF to not create a table for this Type (We only want to query with it)
		// see https://stackoverflow.com/questions/60076606/net-core-3-x-keyless-entity-types-avoid-table-creation
		modelBuilder.Entity<CachedDayStats>().HasNoKey().ToTable(null);
		modelBuilder.Entity<PlaytimeDto>().HasNoKey().ToTable(null);

		// *** Nightly

		//modelBuilder.Entity<NightlyProject>();

		modelBuilder.Entity<NightlyBranch>()
			.HasKey(x => new { x.Project, x.Branch });
		modelBuilder.Entity<NightlyBranch>()
			.HasOne(branch => branch.NightlyProject)
			.WithMany(project => project.Branches)
			.HasForeignKey(branch => branch.Project);

		modelBuilder.Entity<NightlyBuild>()
			.HasKey(x => new { x.Project, x.Branch, x.Commit });
		modelBuilder.Entity<NightlyBuild>()
			.HasOne(build => build.NightlyBranch)
			.WithMany(branch => branch.Builds)
			.HasForeignKey(build => new { build.Project, build.Branch });

		modelBuilder.Entity<LanguageEntry>()
			.HasKey(x => new { x.Project, x.Language });

		modelBuilder.Entity<LanguageEntry>()
			.HasOne(lang => lang.NightlyProject)
			.WithMany(project => project.Languages)
			.HasForeignKey(lang => lang.Project);
	}
}
