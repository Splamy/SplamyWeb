using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System;
using System.Collections.Generic;

namespace SplamyWeb
{
	public class StatsModel : PageModel
	{
		private readonly LocalDb db;

		public StatsModel(LocalDb db)
		{
			this.db = db;
		}

		public IEnumerable<TabStatsEntry> GetStats()
		{
			return db.TabStatsTable.FindAll();
		}

		public AccumStats GetStatsSum()
		{
			var all = db.TabStatsTable.FindAll();
			var acc = new AccumStats();
			foreach (var e in all)
			{
				acc.TotalUptime += e.Data.TotalUptime ?? TimeSpan.Zero;
			}
			return acc;
		}
	}

	public class AccumStats
	{
		public TimeSpan TotalUptime = TimeSpan.Zero;
	}
}
