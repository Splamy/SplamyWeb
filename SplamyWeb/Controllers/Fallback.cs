using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace SplamyWeb.Controllers
{
	[AllowAnonymous]
	public class Fallback(IWebHostEnvironment appEnvironment)
		: ControllerBase
	{
		private static readonly char[] PathSeparators = ['\\', '/'];

		public IActionResult Index()
		{
			var reqPath = Request.Path.Value;
			if (reqPath is null || reqPath.Contains(".."))
				return NotFound();

			const string IndexEndStrip = "index.html";
			if (reqPath.EndsWith(IndexEndStrip, StringComparison.OrdinalIgnoreCase))
				reqPath = reqPath[..^IndexEndStrip.Length];

			try
			{
				var foundFilePath = TryFindFilePath(reqPath);
				if (foundFilePath is not null)
				{
					var targetPath = foundFilePath.AsSpan();
					targetPath = targetPath[appEnvironment.WebRootPath.Length..];
					if (targetPath.EndsWith(IndexEndStrip, StringComparison.OrdinalIgnoreCase))
						targetPath = targetPath[..^IndexEndStrip.Length];
					targetPath = targetPath.TrimEnd([ '\\', '/' ]);
					return RedirectPermanent(targetPath.ToString().Replace('\\', '/') + Request.QueryString);
				}
			}
			catch { }

			return Redirect("/index.html?req=" + Uri.EscapeDataString(Request.Path.Value ?? ""));
		}

		public string? TryFindFilePath(string reqPath)
		{
			var reqPathSplit = reqPath.Trim(PathSeparators).Split('/', StringSplitOptions.RemoveEmptyEntries);
			var pathParts = reqPathSplit.Take(..^1);
			var filePart = reqPathSplit.Last();

			// May come in as
			// test          => [test.html, test/index.html]
			// test.html     => [test.html]
			// test/         => [test/index.html]
			// test/sub      => [test/sub.html, test/sub/index.html]
			// test/sub.html => [test/sub.html]

			var currentPath = new DirectoryInfo(appEnvironment.WebRootPath);
			foreach (var part in pathParts)
			{
				var foundFolder = currentPath.EnumerateDirectories().FirstOrDefault(folder =>
				{
					return string.Equals(folder.Name, part, StringComparison.OrdinalIgnoreCase);
				});
				if (foundFolder is null)
					return null;
				currentPath = foundFolder;
			}

			var rawHtmlFile = filePart + ".html";

			var foundFile = currentPath.EnumerateFiles()
				.FirstOrDefault(file => string.Equals(file.Name, rawHtmlFile, StringComparison.OrdinalIgnoreCase));

			if (foundFile is not null)
				return foundFile.FullName;

			var indexFolder = currentPath.EnumerateDirectories()
				.FirstOrDefault(folder => string.Equals(folder.Name, filePart, StringComparison.OrdinalIgnoreCase));

			if (indexFolder is null)
				return null;

			currentPath = indexFolder;
			foundFile = currentPath.GetFiles("index.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();

			if (foundFile is null)
				return null;

			// TODO check if this is final here

			return foundFile.FullName;
		}
	}
}
