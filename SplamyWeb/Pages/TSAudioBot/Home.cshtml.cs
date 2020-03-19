using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System;

namespace SplamyWeb.Pages
{
	public class TS3AudioBotModel : PageModel
	{
		private readonly TabBackingData tabData;
		public uint Downloads => tabData.Downloads;
		public uint RunningInstances => tabData.RunningInstances;
		public uint RunningBots => tabData.RunningBots;
		public TimeSpan PlaybackTime => tabData.PlaybackTime;

		public TS3AudioBotModel(TabBackingData tabData)
		{
			this.tabData = tabData;
		}

		static readonly string[] ImpMod = { "", "K", "M" };

		public string FormatMetric(uint number)
		{
			uint pow = 0;
			while (number > 1000 && pow <= ImpMod.Length)
			{
				pow++;
				number /= 1000;
			}
			return $"{number}{ImpMod[pow]}";
		}

		public string FormatTime(TimeSpan time)
		{
			if (time < TimeSpan.FromMinutes(1))
				return $"{(int)time.TotalSeconds} sec";
			if (time < TimeSpan.FromHours(1))
				return $"{(int)time.TotalMinutes} min";
			if (time < TimeSpan.FromDays(1))
			{
				var h = (int)time.TotalHours;
				return $"{h} hour{(h > 1 ? "s" : "")}";
			}
			if (time < TimeSpan.FromDays(365.25))
			{
				var d = (int)time.TotalDays;
				return $"{d} day{(d > 1 ? "s" : "")}";
			}

			var y = time.TotalDays / 365.25;
			return $"{y:0.#} year{(y > 1 ? "s" : "")}";
		}
	}
}
