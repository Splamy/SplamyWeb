using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Pages
{
	public class NightlyModel : PageModel
	{
		private readonly UserManager<LoginData> userManager;
		private readonly LocalDb db;

		public NightlyModel(UserManager<LoginData> userManager, LocalDb db)
		{
			this.userManager = userManager;
			this.db = db;
		}

		public async Task<bool> IsExtented()
		{
			var user = await userManager.GetUserAsync(User);
			return user != null && user.Rank >= UserType.Admin;
		}

		public IEnumerable<NightlyProject> GetNightlyProjects()
		{
			return db.NightlyProjectTable.FindAll();
		}

		public IEnumerable<(NightlyEntry entry, bool active)> GetActives(string project, bool includeInactive)
		{
			if (includeInactive)
			{
				var nm = db.NightlyMetaTable.FindAll().ToArray();
				foreach (var n in nm)
				{
					if(n.Project == null || n.Branch == null)
					{
						var split = n.Id.Split('.', 2);
						n.Project = split[0];
						n.Branch = split[1];
						db.NightlyMetaTable.Update(n);
					}
				}

				return from entry in db.NightlyTable.Find(x => x.Project == project)
					   select (entry, db.NightlyMetaTable.FindById(NightlyMeta.GetId(project, entry.Branch))?.Active == entry.Commit);
			}
			else
			{
				return from meta in db.NightlyMetaTable.Find(x => x.Project == project)
					   select db.NightlyTable.FindById(meta.ToEntryId()) into entry
					   where entry != null
					   select (entry, true);
			}
		}

		public void OnGet()
		{
		}
	}
}
