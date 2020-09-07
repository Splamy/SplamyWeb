using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Pages
{
	public class NightlyModel : PageModel
	{
		private readonly UserManager<LoginData> userManager;
		private readonly SplamyContext db;
		private readonly StoreService store;

		public NightlyModel(UserManager<LoginData> userManager, SplamyContext db, StoreService store)
		{
			this.userManager = userManager;
			this.db = db;
			this.store = store;
		}

		public async Task<bool> IsExtented()
		{
			var user = await userManager.GetUserAsync(User);
			return user?.Rank >= UserType.Admin;
		}

		public ValueTask<string?> TryFetchNotification(string project)
		{
			return store.Get("notify_project_" + project);
		}

		public IAsyncEnumerable<ProjectInfo> GetNightlyProjects(bool includeInactive)
		{
			if (includeInactive)
			{
				return (
					from nProject in db.NightlyProjects
					orderby nProject.ProjectName
					select new ProjectInfo
					{
						Project = nProject,
						Builds =
							from build in db.NightlyBuilds
							where build.Project == nProject.Project
							orderby build.UploadTime
							select new BuildInfo { Build = build, Active = build.NightlyBranch.Active == build.Commit }
					}
				).AsAsyncEnumerable();
			}
			else
			{
				return (
					from nProject in db.NightlyProjects
					orderby nProject.ProjectName
					select new ProjectInfo
					{
						Project = nProject,
						Builds =
							from build in db.NightlyBuilds
							where build.Project == nProject.Project && build.NightlyBranch.Active == build.Commit
							orderby build.Branch
							select new BuildInfo { Build = build, Active = true }
					}
				).AsAsyncEnumerable();
			}
		}

		public void OnGet()
		{
		}
	}

	public class ProjectInfo
	{
		public NightlyProject Project { get; set; }
		public IEnumerable<BuildInfo> Builds { get; set; }

		public void Deconstruct(out NightlyProject project, out IEnumerable<BuildInfo> builds)
		{
			project = Project;
			builds = Builds;
		}
	}

	public class BuildInfo
	{
		public NightlyBuild Build { get; set; }
		public bool Active { get; set; }

		public void Deconstruct(out NightlyBuild build, out bool active)
		{
			build = Build;
			active = Active;
		}
	}
}
