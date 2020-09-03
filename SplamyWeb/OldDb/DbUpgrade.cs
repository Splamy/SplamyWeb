using LiteDB;
using RateMapSeveritySaber;
using SplamyWeb.Components;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SplamyWeb.OldDb
{
	public static class DbUpgrade
	{
		public static void DoRamses(LocalDb db, ILiteCollection<RamsesEntry> ramsesTable)
		{
			var context = db.Context;

			// Ramses
			var oldRamses = ramsesTable.FindAll().ToArray();
			var newRamses = new List<Db.RamsesEntry>();

			foreach (var old in oldRamses)
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
				newRamses.Add(entry);
			}

			newRamses.RemoveAll(e => e.Maps.GroupBy(x => x.Characteristic).Count() > 1);

			newRamses.Sort((a, b) => (int)(a.Id - b.Id));
			context.RamsesEntries.AddRange(newRamses);
		}
	}
}
