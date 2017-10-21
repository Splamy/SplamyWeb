using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;

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

			if (project.Contains(".") || branch.Contains(".")) // Sanity check
				return NotFound();

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
			var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return File(stream, MediaTypeNames.Application.Octet, entry.FileName);
		}

		[HttpGet("{project}/{branch}")]
		public IActionResult GetInfo(string project, string branch) // ts3ab/master
		{
			project = project.ToLower();
			branch = branch.ToLower();

			var entry = GetActive(project, branch);
			if (entry == null)
				return NotFound();
			return Ok(entry);
		}

		[HttpGet("{project}")]
		public IActionResult FindBranches(string project)
		{
			project = project.ToLower();

			var entry = LocalDb.NightlyTable.Find(x => x.Project == project).Select(x => x.Branch).Distinct();
			return Ok(entry);
		}

		[HttpPut("{project}/{branch}")]
		public IActionResult Put(string project, string branch,
			[FromQuery] string token,
			[FromQuery] string fileName,
			[FromQuery] string version,
			[FromQuery] string commit
			)
		{
			var user = LocalDb.GetUserByToken(token);
			if (user == null || user.Rank < UserType.Admin)
				return BadRequest("Not authorized");

			project = project.ToLower();
			branch = branch.ToLower();

			if (project.Contains(".") || branch.Contains(".")) // Sanity check
				return BadRequest("Invalid path");

			if (HttpContext.Request.ContentType != MediaTypeNames.Application.Octet &&
				HttpContext.Request.ContentType != MediaTypeNames.Application.Zip)
				return BadRequest("Invalid type");

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

		public void Delete(NightlyEntry entry)
		{

		}

		private static object StripEntry(NightlyEntry entry) => new
		{
			Project = entry.Project,
			Branch = entry.Branch,
			Version = entry.Version,
			Commit = entry.Commit,
		};

		public static string ToActive(string project, string branch) => $"{project}.{branch}";
		public static string ActiveToId(string active, string commit) => $"{active}.{commit}";
		public static string ToId(string project, string branch, string commit) => $"{project}.{branch}.{commit}";
	}
}
