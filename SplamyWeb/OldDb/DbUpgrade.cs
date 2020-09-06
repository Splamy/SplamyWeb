using AutoMapper;
using LiteDB;
using RateMapSeveritySaber;
using SplamyWeb.Components;
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
			var newTable = new List<Db.RamsesEntry>();

			foreach (var old in oldTable)
			{
				byte diffIndex = 0;

				var entry = new Db.RamsesEntry(long.Parse(old.Id, NumberStyles.HexNumber), old.Version);
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
			await context.RamsesEntries.AddRangeAsync(newTable);
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
			var newTable = new List<TabStatsEntryDto>();

			var beforeBug = new DateTime(2020, 3, 17);
			foreach (var old in oldTable)
			{
				if (old.Time <= beforeBug) continue;
				var dto = mapper.Map<TabStatsData, TabStatsEntryDto>(old.Data);
				dto.Time = old.Time;
				newTable.Add(dto);
			}

			await context.TabStatsTable.AddRangeAsync(newTable);
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
	}
}
