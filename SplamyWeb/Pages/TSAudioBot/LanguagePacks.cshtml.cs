using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;

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
