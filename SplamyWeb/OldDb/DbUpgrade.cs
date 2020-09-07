using AutoMapper;
using LiteDB;
using RateMapSeveritySaber;
using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.OldDb
{
	public static class DbUpgrade
	{
		public static async Task DoRamses(SplamyContext context, ILiteCollection<RamsesEntry> ramsesTable)
		{
			var oldTable = ramsesTable.FindAll().ToArray();
			var newTable = new List<RamsesSong>();

			foreach (var old in oldTable)
			{
				byte diffIndex = 0;

				var entry = new RamsesSong(long.Parse(old.Id, NumberStyles.HexNumber), old.Version);
				entry.Maps.AddRange(old.Maps.Select(x => new Db.RamsesMap(
					MapCharacteristic.Standard,
					diffIndex++,
					(byte)BSMapUtil.DifficultyNameToNumber(x.Difficulty),
					x.MaxDifficulty,
					x.AvgDifficulty,
					x.Graph)));
				newTable.Add(entry);
			}

			newTable.RemoveAll(e => e.Maps.GroupBy(x => x.Characteristic).Count() > 1);

			newTable.Sort((a, b) => (int)(a.Id - b.Id));
			await context.RamsesSongs.AddRangeAsync(newTable);
		}

		public static async Task DoStore(SplamyContext context, ILiteCollection<StoreEntry> storeTable)
		{
			var oldTable = storeTable.FindAll().ToArray();
			var newTable = new List<StoreEntry>();

			foreach (var old in oldTable)
			{
				newTable.Add(new StoreEntry(old.Id, old.Value));
			}

			await context.StoreTable.AddRangeAsync(newTable);
		}

		public static async Task DoTabStats(SplamyContext context, IMapper mapper, ILiteCollection<TabStatsEntry> tabStats)
		{
			var oldTable = tabStats.FindAll().ToArray();
			var newTable = new List<TabStatsPingDto>();

			var beforeBug = new DateTime(2020, 3, 17);
			foreach (var old in oldTable)
			{
				if (old.Time <= beforeBug) continue;
				var dto = mapper.Map<TabStatsData, TabStatsPingDto>(old.Data);
				dto.Time = old.Time;
				newTable.Add(dto);
			}

			await context.TabStatsPings.AddRangeAsync(newTable);
		}

		public static async Task DoUserLogin(SplamyContext context, IMapper mapper, ILiteCollection<LoginData> loginTable)
		{
			var oldTable = loginTable
				.FindAll()
				.Select(x => mapper.Map<LoginData, Db.LoginData>(x))
				.Select(x => { x.Id = 0; return x; })
				.ToList();

			await context.User.AddRangeAsync(oldTable);
		}

		public static async Task DoNightly(SplamyContext context, IMapper mapper,
			ILiteCollection<NightlyEntry> NightlyTable,
			ILiteCollection<NightlyMeta> NightlyMetaTable,
			ILiteCollection<NightlyProject> NightlyProjectTable,
			ILiteCollection<LanguageEntry> LanguageTable)
		{
			var nProject = NightlyProjectTable
				.FindAll()
				.Select(x => mapper.Map<NightlyProject, Db.NightlyProject>(x))
				.ToList();
			var nProjHash = new HashSet<string>(nProject.Select(x => x.Project));
			var nNightly = NightlyTable
				.FindAll()
				.Where(x => nProjHash.Contains(x.Project))
				.Select(x => (old: x, nw: mapper.Map<NightlyEntry, Db.NightlyBuild>(x)))
				.ToList();
			var metaDict = NightlyMetaTable
				.FindAll()
				.Select(x => mapper.Map<NightlyMeta, Db.NightlyBranch>(x))
				.ToDictionary(x => x.Branch);

			foreach (var (old, nw) in nNightly)
			{
				if (metaDict.ContainsKey(nw.Branch))
					continue;
				metaDict.Add(nw.Branch, new NightlyBranch()
				{
					Active = null,
					Project = old.Project,
					Branch = nw.Branch,
				});
			}
			var nLang = LanguageTable
				.FindAll()
				.Select(x => mapper.Map<LanguageEntry, Db.LanguageEntry>(x))
				.ToList();

			await context.NightlyProjects.AddRangeAsync(nProject);
			await context.NightlyBranches.AddRangeAsync(metaDict.Values);
			await context.NightlyBuilds.AddRangeAsync(nNightly.Select(x => x.nw));
			await context.LanguageEntries.AddRangeAsync(nLang);
		}
	}
}
