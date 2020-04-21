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

		static readonly string[] ImpMod = { "", "K", "M", "G" };

		public string FormatMetric(uint number)
		{
			uint pow = number > 0 ? (uint)Math.Log10(number) : 0;
			string unit = ImpMod[pow / 3];
			double trimmedNumber = number / Math.Pow(1000, pow / 3);

			return $"{trimmedNumber:0.#}{unit}";
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
			const double avgDaysPerYear = 365.2425;
			if (time < TimeSpan.FromDays(avgDaysPerYear))
			{
				var d = (int)time.TotalDays;
				return $"{d} day{(d > 1 ? "s" : "")}";
			}

			var y = time.TotalDays / avgDaysPerYear;
			return $"{y:0.#} year{(y > 1 ? "s" : "")}";
		}
	}
}
