using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Db;
using System.Collections.Generic;
using System.Linq;

namespace SplamyWeb
{
	public class TabLanguagePacksModel : PageModel
	{
		private readonly SplamyContext db;

		public TabLanguagePacksModel(SplamyContext db)
		{
			this.db = db;
		}

		public IAsyncEnumerable<LanguageEntry> GetLanguages()
		{
			return (
				from lang in db.LanguageEntries
				where lang.Project == "ts3ab"
				select lang).AsAsyncEnumerable();
		}
	}
}
