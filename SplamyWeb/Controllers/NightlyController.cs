using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Db;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[Authorize(AuthenticationSchemes = AuthScheme)] // , Roles = "Admin"
	public class NightlyController : Controller
	{
		private readonly SplamyContext db;
		private static readonly string[] AcceptedContentTypes =
		{
			MediaTypeNames.Application.Octet, // Binary
			MediaTypeNames.Application.Zip,
			"application/gzip"
		};

		public NightlyController(SplamyContext db)
		{
			this.db = db;
		}

		private readonly string nightlyPath = Path.Combine(OldDb.LocalDb.DataPath, "nightly");

		[HttpGet("{project}/{branch}/download")]
		[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Zip)]
		[AllowAnonymous]
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

		[HttpGet("{project}/{branch}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetInfo(string project, string branch)
		{
			project = project.ToLowerInvariant();
			branch = branch.ToLowerInvariant();

			var entry = await GetActive(project, branch);
			if (entry is null)
				return NotFound();
			return Ok(entry.Strip());
		}

		[HttpGet("{project}")]
		[AllowAnonymous]
		public async Task<IActionResult> ProjectInfo(string project)
		{
			project = project.ToLowerInvariant();

			var nProject = await db.NightlyProjects.SingleOrDefaultAsync(np => np.Project == project);
			if (nProject is null)
				return NotFound();
			var branches = await (
				from nb in db.NightlyBranches
				where nb.Project == project
				select nb.Branch)
				.Distinct()
				.ToArrayAsync();

			return Ok(new { name = nProject.ProjectName, branches, });
		}

		[HttpPut("{project}")]
		public async Task<IActionResult> CreateProjectApi(string project)
		{
			return Ok(await CreateProject(project));
		}

		private async Task<NightlyProject> CreateProject(string project)
		{
			project = project.ToLowerInvariant();

			var nProject = await (
				from np in db.NightlyProjects
				where np.Project == project
				select np)
				.SingleOrDefaultAsync();
			if (nProject != null)
				return nProject;

			return (await db.NightlyProjects.AddAsync(new NightlyProject() { Project = project })).Entity;
		}

		[HttpPatch("{project}")]
		public async Task<IActionResult> SetProjectProperties(string project,
			[FromQuery] string name,
			[FromQuery] string commit_url)
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
			return Ok(projData);
		}

		[HttpPut("{project}/{branch}")]
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

			await CreateProject(project);

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
			nBuild ??= (await db.NightlyBuilds.AddAsync(new NightlyBuild { Branch = branch, ZipContent = false, })).Entity;
			nBuild.UploadTime = DateTime.UtcNow;
			nBuild.FileName = fileName ?? defaultName;
			nBuild.Version = version;
			nBuild.Commit = commit;

			var fullPath = new FileInfo(Path.Combine(nightlyPath, project, branch, nBuild.FileName));
			if (Directory.Exists(fullPath.DirectoryName))
				Directory.Delete(fullPath.DirectoryName, true);
			Directory.CreateDirectory(fullPath.DirectoryName);
			using (var demoDataStream = fullPath.OpenWrite())
			{
				await HttpContext.Request.Body.CopyToAsync(demoDataStream);
			}

			await db.SaveChangesAsync();
			return Ok();
		}

		private async Task<NightlyBuild?> GetActive(string project, string branch) => await (
			from nbuild in db.NightlyBuilds
			where nbuild.NightlyBranch.Project == project && nbuild.Branch == branch && nbuild.Commit == nbuild.NightlyBranch.Active
			select nbuild)
			.SingleOrDefaultAsync();

		[HttpDelete("{project}/{branch}")]
		public async Task<IActionResult> Delete(string project, string branch)
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
	}
}
