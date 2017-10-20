using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Controllers;

namespace SplamyWeb.Pages
{
	public class NightlyModel : PageModel
	{
		private readonly UserManager<LoginData> _userManager;

		public NightlyModel(UserManager<LoginData> userManager)
		{
			_userManager = userManager;
		}

		public async Task<bool> IsExtented()
		{
			var user = await _userManager.GetUserAsync(User);
			return user != null && user.Rank > UserType.CoAdmin;
		}

		public static IEnumerable<(NightlyEntry entry, bool active)> GetActives(string project, bool includeInactive)
		{
			if (includeInactive)
			{
				return LocalDb.NightlyTable.Find(x => x.Project == project)
					.Select(x =>
						(x, LocalDb.NightlyMetaTable.FindById(NightlyController.ToActive(project, x.Branch))?.Active == x.Commit));
			}
			else
			{
				var list = LocalDb.NightlyMetaTable.Find(x => x.Project == project).ToArray();
				if(list.Length > 0)
					return list.Select(meta => (LocalDb.NightlyTable.FindById(meta.ToId()), true));
				else
					return LocalDb.NightlyTable.Find(x => x.Project == project).GroupBy(x => x.Branch).Select(x => (x.First(), true));
			}
		}

		public void OnGet()
		{
		}
	}
}