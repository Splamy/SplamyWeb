using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SplamyWeb.Components;

namespace SplamyWeb.Db;

public class SplamyContext(DbContextOptions options, DbContextConfig conf) : DbContext(options)
{
	public required DbSet<RamsesSongDto> RamsesSongs { get; set; }
	public required DbSet<StoreEntry> StoreTable { get; set; }
	public required DbSet<TabStatsPingDto> TabStatsPings { get; set; }
	public required DbSet<TabStatsFactoryDto> TabStatsFactories { get; set; }
	public required DbSet<LoginData> User { get; set; }
	public required DbSet<NightlyProject> NightlyProjects { get; set; }
	public required DbSet<NightlyBranch> NightlyBranches { get; set; }
	public required DbSet<NightlyBuild> NightlyBuilds { get; set; }
	public required DbSet<LanguageEntry> LanguageEntries { get; set; }
	public required DbSet<BlogPost> BlogPosts { get; set; }

	// https://docs.microsoft.com/en-us/ef/core/miscellaneous/logging?tabs=v3
	public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddConsole(); });

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (conf.IsDevelopment)
		{
			optionsBuilder = optionsBuilder.UseLoggerFactory(MyLoggerFactory).EnableSensitiveDataLogging();
		}
		optionsBuilder.UseNpgsql(conf.ConnectionString);
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<LoginData>()
			.HasIndex(user => user.NameNormalized)
			.IsUnique();

		// Ramses ***

		modelBuilder.Entity<RamsesMapDto>()
			.HasKey(x => new { x.RamsesId, x.Characteristic, x.IndexDifficulty });
		modelBuilder.Entity<RamsesMapDto>()
			.HasOne(map => map.RamsesEntry)
			.WithMany(entry => entry.Maps)
			.HasForeignKey(map => map.RamsesId)
			.HasPrincipalKey(entry => entry.Id);

		// *** Tab Stats

		modelBuilder.Entity<TabStatsPingDto>()
			.HasIndex(x => x.Time);
		modelBuilder.Entity<TabStatsFactoryDto>()
			.HasKey(x => new { x.TabStatsId, x.FactoryName });
		modelBuilder.Entity<TabStatsFactoryDto>()
			.HasOne(factory => factory.TabStatsEntry)
			.WithMany(entry => entry.SongStats)
			.HasForeignKey(factory => factory.TabStatsId);

		// This tells EF to not create a table for this Type (We only want to query with it)
		// see https://stackoverflow.com/questions/60076606/net-core-3-x-keyless-entity-types-avoid-table-creation
		modelBuilder.Entity<CachedDayStats>().HasNoKey().ToView(null);
		modelBuilder.Entity<PlaytimeDto>().HasNoKey().ToView(null);

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
		modelBuilder.Entity<NightlyBuild>()
			.HasOne(build => build.NightlyProject)
			.WithMany(project => project.Builds);

		modelBuilder.Entity<LanguageEntry>()
			.HasKey(x => new { x.Project, x.Language });

		modelBuilder.Entity<LanguageEntry>()
			.HasOne(lang => lang.NightlyProject)
			.WithMany(project => project.Languages)
			.HasForeignKey(lang => lang.Project);

		// *** Blog

		modelBuilder.Entity<BlogPost>()
			.Property(b => b.Tags)
			.HasDefaultValueSql("'{}'");
		modelBuilder.Entity<BlogPost>()
			.HasIndex(b => b.CreateTime);
	}

}

public sealed record DbContextConfig(string ConnectionString, bool IsDevelopment);
