using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SplamyWeb.Pages
{
	public class TS3AudioBotModel : PageModel
	{
		public static IEnumerable<LanguageEntry> GetLanguages()
		{
			return LocalDb.LanguageTable.Find(x => x.Project == "ts3ab");
		}
	}
}