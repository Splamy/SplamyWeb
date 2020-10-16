using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	public class LanguageController : Controller
	{
		private static readonly string languageBasePath = Path.Combine(Util.DataPath, "language");

		private readonly SplamyContext db;
		private readonly StoreService store;

		public LanguageController(SplamyContext db, StoreService store)
		{
			this.db = db;
			this.store = store;
		}

		[HttpGet("project/{project}/languages")]
		[Produces(MediaTypeNames.Application.Json)]
		public IActionResult GetLanguageList(string project)
		{
			if (!IsSave(project))
				return BadRequest("Invalid path");

			project = project.ToLowerInvariant();

			var fullPath = new DirectoryInfo(Path.Combine(languageBasePath, project));
			if (!fullPath.Exists)
				return NotFound("The language was not found");

			return Ok(GetLanguageListDir(project));
		}

		private static IEnumerable<string> GetLanguageListDir(string project)
		{
			if (!IsSave(project))
				return Enumerable.Empty<string>();

			project = project.ToLowerInvariant();

			var fullPath = new DirectoryInfo(Path.Combine(languageBasePath, project));
			if (!fullPath.Exists)
				return Enumerable.Empty<string>();

			return fullPath.GetDirectories().Select(d => d.Name);
		}

		[HttpGet("project/{project}/language/{language}/dll")]
		[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Text.Plain)]
		public async Task<IActionResult> GetLanguageFile(string project, string language)
		{
			if (!IsSave(project) || !IsSave(language))
				return BadRequest("Invalid path");

			project = project.ToLowerInvariant();
			CultureInfo culture;
			try { culture = CultureInfo.GetCultureInfo(language); }
			catch { return NotFound("Culture not found"); }

			var langEntry = await GetLanguageEntry(project, culture);
			var fullPath = new FileInfo(Path.Combine(languageBasePath, project, culture.Name, "strings.dll"));
			if (langEntry == null || !fullPath.Exists)
				return NotFound("The language was not found");

			langEntry.DownloadCount++;
			await db.SaveChangesAsync();

			return PhysicalFile(fullPath.FullName, MediaTypeNames.Application.Octet, "strings.dll");
		}

		[HttpPost("project/{project}/update")]
		public async Task<IActionResult> UpdateLanguageFilesAsync(string project)
		{
			// GET https://www.transifex.com/api/2/project/ts3audiobot/languages
			// GET https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/en/?file

			var requestM = await TransifexRequest(HttpMethod.Get, "https://www.transifex.com/api/2/project/ts3audiobot/languages");
			using var resultM = await httpClient.SendAsync(requestM);
			if (!resultM.IsSuccessStatusCode)
				return UnprocessableEntity("Error from transifex");
			var languages = await resultM.Content.ReadFromJsonAsync<TransifexLanguage[]>(JsonDefault);

			var projectPath = Path.Combine(languageBasePath, project);
			Directory.CreateDirectory(projectPath);

			await Task.WhenAll(languages.Select(async lang =>
			{
				var language = lang.language_code;
				var request = await TransifexRequest(HttpMethod.Get, $"https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/{language}/?file");

				using var result = await httpClient.SendAsync(request);
				if (!result.IsSuccessStatusCode)
					return;

				try { language = CultureInfo.GetCultureInfo(language.Replace("_", "-", StringComparison.Ordinal)).Name; }
				catch { return; }

				var languagePath = Path.Combine(projectPath, language);
				Directory.CreateDirectory(languagePath);

				using var demoDataStream = System.IO.File.Open(Path.Combine(languagePath, "strings.resx"), FileMode.Create, FileAccess.Write);
				using var stream = await result.Content.ReadAsStreamAsync();
				await stream.CopyToAsync(demoDataStream);
			}));

			return await RebuildLanguageFiles(project);
		}

		[HttpPost("project/{project}/rebuild")]
		public async Task<IActionResult> RebuildLanguageFiles(string project)
		{
			// GET https://www.transifex.com/api/2/project/ts3audiobot/languages
			// GET https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/en/?file

			var projectPath = Path.Combine(languageBasePath, project);
			Directory.CreateDirectory(projectPath);

			await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE nightly_lang");

			var report = await Task.WhenAll(GetLanguageListDir(project).Select(async langFile =>
			{
				string language;
				try { language = CultureInfo.GetCultureInfo(langFile).Name; }
				catch { return new BuildReport(langFile, false, "Lang not found"); }

				var languagePath = Path.Combine(projectPath, language);
				if (!System.IO.File.Exists(Path.Combine(languagePath, "strings.resx")))
					return new BuildReport(language, false, "strings.resx not found");

				var result = await ProcessFile(
					"resgen",
					"strings.resx",
					language, languagePath,
					"Failed to transform resx file");
				if (result != null) { return result; }

				result = await ProcessFile(
					"al",
					$"-target:lib -embed:strings.resources,TS3AudioBot.Localization.strings.{language}.resources -culture:{language} -out:TS3AudioBot.resources.dll",
					language, languagePath,
					"Failed to compile satellite assembly");
				if (result != null) { return result; }

				System.IO.File.Copy(Path.Combine(languagePath, "TS3AudioBot.resources.dll"), Path.Combine(languagePath, "strings.dll"), true);

				db.LanguageEntries.Add(new LanguageEntry
				{
					Language = language,
					Project = project,
					UploadTime = DateTime.UtcNow
				});

				return new BuildReport(language, true);
			}));

			await db.SaveChangesAsync();
			return Ok(report);
		}

		private static async Task<BuildReport?> ProcessFile(string bin, string arg, string language, string languagePath, string errMsg)
		{
			// TODO 10 sec timout
			var buildRes = await Cli.Wrap(bin)
				.WithArguments(arg)
				.WithWorkingDirectory(languagePath)
				.WithValidation(CommandResultValidation.None)
				.ExecuteBufferedAsync();

			if (buildRes.ExitCode != 0)
			{
				return new BuildReport(language, false, errMsg);
			}
			return null;
		}

		private async ValueTask<HttpRequestMessage> TransifexRequest(HttpMethod method, string link)
		{
			var request = new HttpRequestMessage(method, link);
			var auth = await store.GetTransifexAuth();
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{auth}")));
			return request;
		}

		private async Task<LanguageEntry?> GetLanguageEntry(string project, CultureInfo culture)
		{
			return await (
				from lang in db.LanguageEntries
				where lang.Project == project && lang.Language == culture.Name
				select lang).SingleOrDefaultAsync();
		}
	}

#pragma warning disable IDE1006, CS8618 // Naming Styles
	internal class BuildReport
	{
		public string language { get; set; }
		public bool ok { get; set; }
		public string? message { get; set; }

		public BuildReport(string language, bool ok, string? message = null)
		{
			this.language = language;
			this.ok = ok;
			this.message = message;
		}
	}

	internal class TransifexLanguage
	{
		//public object coordinators { get; set; }
		public string language_code { get; set; }
	}
#pragma warning restore IDE1006, CS8618 // Naming Styles
}
