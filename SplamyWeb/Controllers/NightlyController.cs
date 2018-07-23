using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	public class NightlyController : Controller
	{
		private readonly string nightlyPath = Path.Combine(LocalDb.DataPath, "nightly");

		[HttpGet("{project}/{branch}/download")]
		[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Application.Zip)]
		public IActionResult GetDownload(string project, string branch) // ts3ab/master
		{
			project = project.ToLower();
			branch = branch.ToLower();

			if (!Save(project) || !Save(branch))
				return BadRequest("Invalid path");

			var entry = GetActive(project, branch);
			if (entry == null)
				return NotFound();

			entry.DownloadCount++;
			LocalDb.NightlyTable.Update(entry);

			if (entry.ZipContent)
			{
				return BadRequest("Not supported");
			}

			var path = Path.Combine(nightlyPath, project, branch, entry.FileName);
			return PhysicalFile(path, MediaTypeNames.Application.Octet, entry.FileName);
		}

		[HttpGet("{project}/{branch}")]
		public IActionResult GetInfo(string project, string branch) // ts3ab/master
		{
			project = project.ToLower();
			branch = branch.ToLower();

			var entry = GetActive(project, branch);
			if (entry == null)
				return NotFound();
			return Ok(entry.Strip());
		}

		[HttpGet("{project}")]
		public IActionResult FindBranches(string project)
		{
			project = project.ToLower();

			var name = LocalDb.NightlyProjectTable.FindById(project);
			if (name == null)
				return NotFound();
			var branches = LocalDb.NightlyTable.Find(x => x.Project == project).Select(x => x.Branch).Distinct();

			return Ok(new { name, branches, });
		}

		[HttpPut("{project}")]
		public IActionResult CreateProject(string project, [FromQuery] string token)
		{
			project = project.ToLower();

			var user = LocalDb.GetUserByToken(token);
			if (user == null || user.Rank < UserType.Admin)
				return BadRequest("Not authorized");

			return Ok(CreateProject(project));
		}

		private static NightlyProject CreateProject(string project)
		{
			project = project.ToLower();

			var projData = LocalDb.NightlyProjectTable.FindById(project);
			if (projData != null)
				return projData;

			projData = new NightlyProject() { Id = project };

			LocalDb.NightlyProjectTable.Upsert(projData);

			return projData;
		}

		[HttpPatch("{project}")]
		public IActionResult SetProjectProperties(string project,
			[FromQuery] string token,
			[FromQuery] string name)
		{
			project = project.ToLower();

			var user = LocalDb.GetUserByToken(token);
			if (user == null || user.Rank < UserType.Admin)
				return BadRequest("Not authorized");

			var projData = LocalDb.NightlyProjectTable.FindById(project);
			if (projData == null)
				return NotFound();

			if (name != null)
				projData.ProjectName = name;

			LocalDb.NightlyProjectTable.Upsert(projData);

			return Ok(projData);
		}

		[HttpPut("{project}/{branch}")]
		public IActionResult Put(string project, string branch,
			[FromQuery] string token,
			[FromQuery] string fileName,
			[FromQuery] string version,
			[FromQuery] string commit)
		{
			var user = LocalDb.GetUserByToken(token);
			if (user == null || user.Rank < UserType.Admin)
				return BadRequest("Not authorized");

			project = project.ToLower();
			branch = branch.ToLower();

			if (!Save(project) || !Save(branch))
				return BadRequest("Invalid path");

			if (HttpContext.Request.ContentType != MediaTypeNames.Application.Octet
				&& HttpContext.Request.ContentType != MediaTypeNames.Application.Zip)
				return BadRequest("Invalid type");

			CreateProject(project);

			const string defaultName = "data.dat";
			string id = ToId(project, branch, commit);
			var entry = LocalDb.NightlyTable.FindById(id);
			if (entry == null)
				entry = new NightlyEntry
				{
					Branch = branch,
					Project = project,
					ZipContent = false,
					Id = id,
				};
			entry.UploadTime = DateTime.UtcNow;
			entry.FileName = fileName ?? defaultName;
			entry.Version = version;
			entry.Commit = commit;
			LocalDb.NightlyTable.Upsert(entry);
			var meta = LocalDb.NightlyMetaTable.FindById(ToActive(project, branch));
			if (meta == null)
				meta = new NightlyMeta { Id = ToActive(project, branch), Active = commit, Project = project };
			else
				meta.Active = commit;
			LocalDb.NightlyMetaTable.Upsert(meta);

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

		private static NightlyEntry GetActive(string project, string branch)
		{
			var activeId = ToActive(project, branch);
			var meta = LocalDb.NightlyMetaTable.FindById(activeId);
			if (meta == null)
				return null;
			return LocalDb.NightlyTable.FindById(meta.ToId());
		}

		[HttpDelete("{project}/{branch}")]
		public void Delete(string project, string branch, [FromQuery] string token)
		{
			// ...
		}

		private static void Delete(NightlyEntry entry)
		{

		}

		public static string ToActive(string project, string branch) => $"{project}.{branch}";
		public static string ActiveToId(string active, string commit) => $"{active}.{commit}";
		public static string ToId(string project, string branch, string commit) => $"{project}.{branch}.{commit}";
	}
}
