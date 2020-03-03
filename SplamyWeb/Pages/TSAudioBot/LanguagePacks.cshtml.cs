using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Collections.Generic;

namespace SplamyWeb
{
	public class TabLanguagePacksModel : PageModel
	{
		private readonly LocalDb db;

		public TabLanguagePacksModel(LocalDb db)
		{
			this.db = db;
		}

		public IEnumerable<LanguageEntry> GetLanguages()
		{
			return db.LanguageTable.Find(x => x.Project == "ts3ab");
		}
	}
}
