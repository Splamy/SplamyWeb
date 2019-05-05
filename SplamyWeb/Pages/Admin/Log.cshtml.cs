using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SplamyWeb.Pages
{
	public class LogModel : PageModel
	{
		public void OnGet()
		{
		}

		public IEnumerable<string> GetLogs()
		{
			if (!int.TryParse(Request.Query["off"], out var from))
				from = 0;
			return Util.NLogMemory.Logs.Reverse().Skip(from).Take(50);
		}
	}
}
