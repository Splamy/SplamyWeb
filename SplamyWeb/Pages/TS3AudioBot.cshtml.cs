using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Collections.Generic;

namespace SplamyWeb.Pages
{
	public class TS3AudioBotModel : PageModel
	{
		private readonly LocalDb db;

		public TS3AudioBotModel(LocalDb db)
		{
			this.db = db;
		}

		public IEnumerable<LanguageEntry> GetLanguages()
		{
			return db.LanguageTable.Find(x => x.Project == "ts3ab");
		}
	}
}
