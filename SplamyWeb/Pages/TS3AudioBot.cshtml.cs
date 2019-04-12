using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;

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
