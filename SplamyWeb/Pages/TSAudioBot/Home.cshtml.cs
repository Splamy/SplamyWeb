using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SplamyWeb.Pages
{
	public class TS3AudioBotModel : PageModel
	{
		private readonly TabBackingData tabData;
		public int Downloads => tabData.Downloads;
		public int RunningInstances => tabData.RunningInstances;
		public int RunningBots => tabData.RunningBots;
		public int PlaybackTime => tabData.PlaybackTime;

		public TS3AudioBotModel(TabBackingData tabData)
		{
			this.tabData = tabData;
		}

		static readonly string[] ImpMod = { "", "K", "M" };

		public string FormatMetric(int number)
		{
			int pow = 0;
			while (number > 1000 && pow <= ImpMod.Length)
			{
				pow++;
				number /= 1000;
			}
			return $"{number}{ImpMod[pow]}";
		}
	}
}
