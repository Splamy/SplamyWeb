using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Mime;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthScheme)] // , Roles = "Admin"
[Route("api/[controller]")]
public class NightlyController : ControllerBase
{
	private readonly UserManager<LoginData> userManager;
	private readonly SplamyContext db;
	private readonly StoreService store;

	const int PageBuildCount = 20;

	private static readonly string[] AcceptedContentTypes =
	{
		MediaTypeNames.Application.Octet, // Binary
		MediaTypeNames.Application.Zip,
		"application/gzip"
	};

	public NightlyController(UserManager<LoginData> userManager, SplamyContext db, StoreService store)
	{
		this.userManager = userManager;
		this.db = db;
		this.store = store;
	}

	private readonly string nightlyPath = Path.Combine(Util.DataPath, "nightly");

	[AllowAnonymous]
	[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Zip)]
	[HttpGet("projects/{project}/{branch}/download")]
	public async Task<IActionResult> GetDownload(string project, string branch)
	{
		project = project.ToLowerInvariant();
		branch = branch.ToLowerInvariant();

		if (!IsSave(project) || !IsSave(branch))
			return BadRequest("Invalid path");

		var entry = await GetActive(project, branch);
		if (entry is null)
			return NotFound();

		entry.DownloadCount++;
		await db.SaveChangesAsync();

		if (entry.ZipContent)
		{
			return BadRequest("Not supported");
		}

		var path = Path.Combine(nightlyPath, project, branch, entry.FileName);
		return PhysicalFile(path, MediaTypeNames.Application.Octet, entry.FileName);
	}

	[AllowAnonymous]
	[HttpGet("projects")]
	public async Task<IActionResult> GetProjects([FromQuery] bool includeInactive)
	{
		var isAdmin = await ExtendedPermission();
		var resultList = await GetNightlyProjects(isAdmin && includeInactive, isAdmin).ToListAsync();
		return new JsonResult(resultList, JsonWebHideNull);
	}

	[AllowAnonymous]
	[HttpGet("projects/{project}")]
	public async Task<IActionResult> GetProjectBuilds(string project, [FromQuery] bool includeInactive, [FromQuery] int page = 0)
	{
		project = project.ToLowerInvariant();

		var isAdmin = await ExtendedPermission();
		var resultList = await GetNightlyProjectBuilds(project, isAdmin && includeInactive, isAdmin)
			.Skip(page * PageBuildCount)
			.Take(PageBuildCount)
			.ToListAsync();
		return new JsonResult(resultList, JsonWebHideNull);
	}

	[HttpPut("projects/{project}")]
	public async Task<IActionResult> CreateProjectApi(string project,
		[FromQuery] string? name,
		[FromQuery] string? commit_url)
	{
		project = project.ToLowerInvariant();

		var nProject = await (
			from np in db.NightlyProjects
			where np.Project == project
			select np)
			.SingleOrDefaultAsync();
		if (nProject != null)
			return Ok();

		nProject = new NightlyProject() { Project = project };
		nProject.ProjectName = name ?? project;
		nProject.CommitUrl = commit_url ?? ""; // TODO real null ?

		await db.NightlyProjects.AddAsync(nProject);
		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpPatch("projects/{project}")]
	public async Task<IActionResult> SetProjectProperties(string project,
		[FromQuery] string? name,
		[FromQuery] string? commit_url)
	{
		project = project.ToLowerInvariant();

		var projData = await db.NightlyProjects.SingleOrDefaultAsync(np => np.Project == project);
		if (projData is null)
			return NotFound();

		if (name != null)
			projData.ProjectName = name;
		if (commit_url != null)
			projData.CommitUrl = commit_url;

		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpPut("projects/{project}/{branch}")]
	[RequestSizeLimit(100_000_000)]
	public async Task<IActionResult> Put(string project, string branch,
		[FromQuery] string fileName,
		[FromQuery] string version,
		[FromQuery] string commit)
	{
		project = project.ToLowerInvariant();
		branch = branch.ToLowerInvariant();

		if (!IsSave(project) || !IsSave(branch))
			return BadRequest("Invalid path");

		if (!AcceptedContentTypes.Contains(HttpContext.Request.ContentType))
			return BadRequest("Invalid type");

		if (!await db.NightlyProjects.AnyAsync(nProject => nProject.Project == project))
			return BadRequest("Project does not exist");

		const string defaultName = "data.dat";

		var nBranch = await (
			from nb in db.NightlyBranches
			where nb.Project == project && nb.Branch == branch
			select nb)
			.SingleOrDefaultAsync();
		nBranch ??= (await db.NightlyBranches.AddAsync(new NightlyBranch { Project = project, Branch = branch })).Entity;
		nBranch.Active = commit;

		var nBuild = await (
			from nb in db.NightlyBuilds
			where nb.NightlyBranch.Project == project && nb.Branch == branch && nb.Commit == commit
			select nb)
			.SingleOrDefaultAsync();

		nBuild ??= (await db.NightlyBuilds.AddAsync(new NightlyBuild { Project = project, Branch = branch, Commit = commit, ZipContent = false, })).Entity;
		nBuild.UploadTime = DateTime.UtcNow;
		nBuild.FileName = fileName ?? defaultName;
		nBuild.Version = version;

		var fullPath = new FileInfo(Path.Combine(nightlyPath, project, branch, nBuild.FileName));
		var dir = fullPath.Directory;
		if (dir is null)
			return Problem("Could not create directory");
		if (dir.Exists)
			dir.Delete(true);
		dir.Create();
		using (var demoDataStream = fullPath.OpenWrite())
		{
			await HttpContext.Request.Body.CopyToAsync(demoDataStream);
		}

		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpDelete("projects/{project}")]
	public async Task<IActionResult> DeleteProject(string project)
	{
		var nProject = await (
			from nb in db.NightlyProjects
			where nb.Project == project
			select nb)
			.SingleOrDefaultAsync();
		if (nProject is null)
			return NotFound();
		db.NightlyProjects.Remove(nProject);
		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpDelete("projects/{project}/{branch}")]
	public async Task<IActionResult> DeleteProjectBranch(string project, string branch)
	{
		var nBranch = await (
			from nb in db.NightlyBranches
			where nb.Project == project && nb.Branch == branch
			select nb)
			.SingleOrDefaultAsync();
		if (nBranch is null)
			return StatusCode(304);
		nBranch.Active = null;
		await db.SaveChangesAsync();
		return Ok();
	}

	private async Task<NightlyBuild?> GetActive(string project, string branch) => await (
		from nbuild in db.NightlyBuilds
		where nbuild.NightlyBranch.Project == project && nbuild.Branch == branch && nbuild.Commit == nbuild.NightlyBranch.Active
		select nbuild)
		.SingleOrDefaultAsync();

	private ValueTask<string?> TryFetchNotification(string project)
	{
		return store.Get("notify_project_" + project);
	}

	private async Task<bool> ExtendedPermission()
	{
		var user = await userManager.GetUserAsync(User);
		if (user is null)
			return false;

		return user.Rank.AtLeast(UserType.Admin);
	}

	private IQueryable<ProjectInfo> GetNightlyProjects(bool includeInactive, bool isAdmin)
	{
		Expression<Func<NightlyBuild, object>> orderBy;
		if (includeInactive)
			orderBy = build => build.UploadTime;
		else
			orderBy = build => build.Branch;

		IQueryable<ProjectInfoMapper> query = (
			from nProject in db.NightlyProjects
			orderby nProject.ProjectName
			select new ProjectInfoMapper
			{
				NightlyProject = nProject,
				Notification = db.StoreTable
					.Where(kvp => kvp.Id == "notify_project_" + nProject.Project)
					.Select(kvp => kvp.Value)
					.AsSplitQuery()
					.FirstOrDefault(),
				Builds = db.NightlyBuilds
					.Where(build => build.Project == nProject.Project && (includeInactive || build.NightlyBranch.Active == build.Commit))
					.OrderBy(orderBy)
					.Select(build => new BuildInfoMapper { Build = build, Active = (!includeInactive || build.NightlyBranch.Active == build.Commit) })
					.Take(PageBuildCount)
					.ToList(),
				BuildCount = db.NightlyBuilds.Count(build => build.Project == nProject.Project && (includeInactive || build.NightlyBranch.Active == build.Commit))
			});
		IQueryable<ProjectInfo> mappedQuery = query.ProjectTo<ProjectInfo>(isAdmin ? AdminMapping : UserMapping);
		return mappedQuery;
	}

	private IQueryable<BuildInfo> GetNightlyProjectBuilds(string project, bool includeInactive, bool isAdmin)
	{
		Expression<Func<NightlyBuild, object>> orderBy;
		if (includeInactive)
			orderBy = build => build.UploadTime;
		else
			orderBy = build => build.Branch;

		IQueryable<BuildInfoMapper> query = db.NightlyBuilds
			.Where(build => build.Project == project && (includeInactive || build.NightlyBranch.Active == build.Commit))
			.OrderBy(orderBy)
			.Select(build => new BuildInfoMapper { Build = build, Active = (!includeInactive || build.NightlyBranch.Active == build.Commit) });

		IQueryable<BuildInfo> mappedQuery = query.ProjectTo<BuildInfo>(isAdmin ? AdminMapping : UserMapping);
		return mappedQuery;
	}

	static readonly MapperConfiguration AdminMapping = new(cfg =>
	{
		cfg.CreateMap<NightlyProject, ProjectInfo>(MemberList.None)
			.ForMember(x => x.Extended, opt => opt.MapFrom((_) => true));
		cfg.CreateMap<NightlyBuild, BuildInfo>(MemberList.None);

		cfg.CreateMap<ProjectInfoMapper, ProjectInfo>(MemberList.None)
			.IncludeMembers(src => src.NightlyProject);
		cfg.CreateMap<BuildInfoMapper, BuildInfo>(MemberList.Destination)
			.IncludeMembers(src => src.Build);
	});

	static readonly MapperConfiguration UserMapping = new(cfg =>
	{
		cfg.CreateMap<NightlyProject, ProjectInfo>(MemberList.None)
			.ForMember(x => x.Extended, opt => opt.MapFrom((_) => (bool?)null));
		cfg.CreateMap<NightlyBuild, BuildInfo>(MemberList.None)
			.ForMember(x => x.Active, opt => opt.MapFrom((_) => (bool?)null))
			.ForMember(x => x.DownloadCount, opt => opt.MapFrom((_) => (int?)null));

		cfg.CreateMap<ProjectInfoMapper, ProjectInfo>(MemberList.None)
			.IncludeMembers(src => src.NightlyProject);
		cfg.CreateMap<BuildInfoMapper, BuildInfo>(MemberList.Destination)
			.IncludeMembers(src => src.Build)
			.ForMember(x => x.Active, opt => opt.MapFrom((_) => (bool?)null));
	});
}


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class ProjectInfoMapper
{
	public NightlyProject NightlyProject { get; set; }
	public string? Notification { get; set; }
	public IList<BuildInfoMapper> Builds { get; set; }
	public int BuildCount { get; set; }
};

public record ProjectInfo
{
	public string Project { get; set; }
	public string ProjectName { get; set; }
	public string CommitUrl { get; set; }
	public string? Notification { get; set; }
	public bool? Extended { get; set; }
	public IList<BuildInfo> Builds { get; set; }
	public int BuildCount { get; set; }
}

public record BuildInfoMapper
{
	public NightlyBuild Build { get; set; }
	public bool? Active { get; set; }
}

public class BuildInfo
{
	public bool? Active { get; set; }
	public string Branch { get; set; }
	public string Commit { get; set; }
	public string Version { get; set; }
	public bool ZipContent { get; set; }
	public string FileName { get; set; }
	public DateTime UploadTime { get; set; }
	public int? DownloadCount { get; set; }

}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
