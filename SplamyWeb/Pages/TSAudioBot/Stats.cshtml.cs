using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;

namespace SplamyWeb
{
	public class StatsModel : PageModel
	{
		private readonly LocalDb db;

		public StatsModel(LocalDb db)
		{
			this.db = db;
		}

		public AccumStats GetStats()
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
