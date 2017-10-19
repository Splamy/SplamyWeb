using Microsoft.AspNetCore.Mvc;
using System.IO;
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

			var entry = LocalDb.NightlyTable.FindOne(x => x.Project == project && x.Branch == branch);
			if (entry == null)
				return NotFound();

			if (project.Contains(".") || branch.Contains(".")) // Sanity check
				return NotFound();

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

			var entry = LocalDb.NightlyTable.FindOne(x => x.Project == project && x.Branch == branch);
			if (entry == null)
				return NotFound();

			return Ok(entry);
		}

		[HttpGet("{project}")]
		public IActionResult FindBranches(string project)
		{
			project = project.ToLower();

			var entry = LocalDb.NightlyTable.Find(x => x.Project == project);
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
			if(user == null || user.Rank < UserType.Admin)
				return BadRequest("Not authorized");

			project = project.ToLower();
			branch = branch.ToLower();

			if (project.Contains(".") || branch.Contains(".")) // Sanity check
				return BadRequest("Invalid path");

			if (HttpContext.Request.ContentType != MediaTypeNames.Application.Octet &&
				HttpContext.Request.ContentType != MediaTypeNames.Application.Zip)
				return BadRequest("Invalid type");

			const string defaultName = "data.dat";
			var entry = new NightlyEntry
			{
				Branch = branch,
				Project = project,
				FileName = fileName ?? defaultName,
				Version = version,
				Commit = commit,
				ZipContent = false,
				Id = $"{project}.{branch}",
			};
			LocalDb.NightlyTable.Upsert(entry);

			var fullPath = new FileInfo(Path.Combine(nightlyPath, project, branch, entry.FileName));
			if(Directory.Exists(fullPath.DirectoryName))
				Directory.Delete(fullPath.DirectoryName, true);
			Directory.CreateDirectory(fullPath.DirectoryName);
			using (var demoDataStream = fullPath.OpenWrite())
			{
				HttpContext.Request.Body.CopyTo(demoDataStream);
			}

			return Ok();
		}

		[HttpDelete("{project}/{branch}")]
		public void Delete(string project, string branch, [FromQuery] string token)
		{

		}
	}
}
