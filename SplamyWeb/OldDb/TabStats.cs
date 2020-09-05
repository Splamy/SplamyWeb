using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.OldDb
{
	public class TabStatsEntry
	{
		public long Id { get; set; }
		public DateTime Time { get; set; }
		public TabStatsData Data { get; set; }

		public TabStatsEntry(long id, DateTime time) : this(id, time, new TabStatsData()) { }

		public TabStatsEntry(long id, DateTime time, TabStatsData data)
		{
			Id = id;
			Time = time;
			Data = data;
		}
	}
}
