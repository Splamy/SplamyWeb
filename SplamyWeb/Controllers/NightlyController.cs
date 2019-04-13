using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[Authorize(AuthenticationSchemes = "BasicAuthentication,Identity.Application")] // , Roles = "Admin"
	public class NightlyController : Controller
	{
		private readonly LocalDb db;

		public NightlyController(LocalDb db)
		{
			this.db = db;
		}

		private readonly string nightlyPath = Path.Combine(LocalDb.DataPath, "nightly");

		[HttpGet("{project}/{branch}/download")]
		[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Zip)]
		[AllowAnonymous]
		public IActionResult GetDownload(string project, string branch)
		{
			project = project.ToLower();
			branch = branch.ToLower();

			if (!IsSave(project) || !IsSave(branch))
				return BadRequest("Invalid path");

			var entry = GetActive(project, branch);
			if (entry == null)
				return NotFound();

			entry.DownloadCount++;
			db.NightlyTable.Update(entry);

			if (entry.ZipContent)
			{
				return BadRequest("Not supported");
			}

			var path = Path.Combine(nightlyPath, project, branch, entry.FileName);
			return PhysicalFile(path, MediaTypeNames.Application.Octet, entry.FileName);
		}

		[HttpGet("{project}/{branch}")]
		[AllowAnonymous]
		public IActionResult GetInfo(string project, string branch)
		{
			project = project.ToLower();
			branch = branch.ToLower();

			var entry = GetActive(project, branch);
			if (entry == null)
				return NotFound();
			return Ok(entry.Strip());
		}

		[HttpGet("{project}")]
		[AllowAnonymous]
		public IActionResult FindBranches(string project)
		{
			project = project.ToLower();

			var name = db.NightlyProjectTable.FindById(project);
			if (name == null)
				return NotFound();
			var branches = db.NightlyTable.Find(x => x.Project == project).Select(x => x.Branch).Distinct();

			return Ok(new { name, branches, });
		}

		[HttpPut("{project}")]
		public IActionResult CreateProjectApi(string project)
		{
			return Ok(CreateProject(project));
		}

		private NightlyProject CreateProject(string project)
		{
			project = project.ToLower();

			var projData = db.NightlyProjectTable.FindById(project);
			if (projData != null)
				return projData;

			projData = new NightlyProject() { Id = project };

			db.NightlyProjectTable.Upsert(projData);

			return projData;
		}

		[HttpPatch("{project}")]
		public IActionResult SetProjectProperties(string project,
			[FromQuery] string name,
			[FromQuery] string commit_url)
		{
			project = project.ToLower();

			var projData = db.NightlyProjectTable.FindById(project);
			if (projData == null)
				return NotFound();

			if (name != null)
				projData.ProjectName = name;
			if (commit_url != null)
				projData.CommitUrl = commit_url;

			db.NightlyProjectTable.Update(projData);

			return Ok(projData);
		}

		[HttpPut("{project}/{branch}")]
		[RequestSizeLimit(100_000_000)]
		public IActionResult Put(string project, string branch,
			[FromQuery] string fileName,
			[FromQuery] string version,
			[FromQuery] string commit)
		{
			project = project.ToLower();
			branch = branch.ToLower();

			if (!IsSave(project) || !IsSave(branch))
				return BadRequest("Invalid path");

			if (HttpContext.Request.ContentType != MediaTypeNames.Application.Octet
				&& HttpContext.Request.ContentType != MediaTypeNames.Application.Zip)
				return BadRequest("Invalid type");

			CreateProject(project);

			const string defaultName = "data.dat";
			string id = NightlyEntry.GetId(project, branch, commit);
			var entry = db.NightlyTable.FindById(id);
			if (entry == null)
				entry = new NightlyEntry
				{
					Branch = branch,
					Project = project,
					ZipContent = false,
				};
			entry.UploadTime = DateTime.UtcNow;
			entry.FileName = fileName ?? defaultName;
			entry.Version = version;
			entry.Commit = commit;
			db.NightlyTable.Upsert(entry);
			var meta = db.NightlyMetaTable.FindById(NightlyMeta.GetId(project, branch));
			if (meta == null)
				meta = new NightlyMeta { Active = commit, Project = project, Branch = branch };
			else
				meta.Active = commit;
			db.NightlyMetaTable.Upsert(meta);

			var fullPath = new FileInfo(Path.Combine(nightlyPath, project, branch, entry.FileName));
			if (Directory.Exists(fullPath.DirectoryName))
				Directory.Delete(fullPath.DirectoryName, true);
			Directory.CreateDirectory(fullPath.DirectoryName);
			using (var demoDataStream = fullPath.OpenWrite())
			{
				HttpContext.Request.Body.CopyTo(demoDataStream);
			}

			return Ok();
		}

		private NightlyEntry GetActive(string project, string branch)
		{
			var activeId = NightlyMeta.GetId(project, branch);
			var meta = db.NightlyMetaTable.FindById(activeId);
			if (meta == null)
				return null;
			return db.NightlyTable.FindById(meta.ToEntryId());
		}

		[HttpDelete("{project}/{branch}")]
		public IActionResult Delete(string project, string branch)
		{
			var deleted = db.NightlyMetaTable.Delete(NightlyMeta.GetId(project, branch));
			if (!deleted)
				return StatusCode(304);
			return Ok();
		}
	}
}
