using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SplamyWeb.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	public class LanguageController : Controller
	{
		private static readonly string languageBasePath = Path.Combine(LocalDb.DataPath, "language");

		private readonly LocalDb db;

		public LanguageController(LocalDb db)
		{
			this.db = db;
		}

		[HttpGet("project/{project}/languages")]
		[Produces(MediaTypeNames.Application.Json)]
		public IActionResult GetLanguageList(string project)
		{
			if (!IsSave(project))
				return BadRequest("Invalid path");

			project = project.ToLower();

			var fullPath = new DirectoryInfo(Path.Combine(languageBasePath, project));
			if (!fullPath.Exists)
				return NotFound("The language was not found");

			return Ok(GetLanguageListDir(project));
		}

		private static IEnumerable<string> GetLanguageListDir(string project)
		{
			if (!IsSave(project))
				return Enumerable.Empty<string>();

			project = project.ToLower();

			var fullPath = new DirectoryInfo(Path.Combine(languageBasePath, project));
			if (!fullPath.Exists)
				return Enumerable.Empty<string>();

			return fullPath.GetDirectories().Select(d => d.Name);
		}

		[HttpGet("project/{project}/language/{language}/dll")]
		[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Text.Plain)]
		public IActionResult GetLanguageFile(string project, string language)
		{
			if (!IsSave(project) || !IsSave(language))
				return BadRequest("Invalid path");

			project = project.ToLower();
			CultureInfo culture;
			try { culture = CultureInfo.GetCultureInfo(language); }
			catch { return NotFound("Culture not found"); }

			var langEntry = GetLanguageEntry(project, culture);
			var fullPath = new FileInfo(Path.Combine(languageBasePath, project, culture.Name, "strings.dll"));
			if (langEntry == null || !fullPath.Exists)
				return NotFound("The language was not found");

			langEntry.DownloadCount++;
			db.LanguageTable.Upsert(langEntry);

			return PhysicalFile(fullPath.FullName, MediaTypeNames.Application.Octet, "strings.dll");
		}

		[HttpPost("project/{project}/update")]
		public async Task<IActionResult> UpdateLanguageFilesAsync(string project, [FromQuery] string transifex)
		{
			// GET https://www.transifex.com/api/2/project/ts3audiobot/languages
			// GET https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/en/?file

			using (var client = HttpClientFactory.Create())
			{
				var requestM = TransifexRequest(HttpMethod.Get, "https://www.transifex.com/api/2/project/ts3audiobot/languages", transifex);
				var resultM = await client.SendAsync(requestM);
				if (!resultM.IsSuccessStatusCode)
					return UnprocessableEntity("Error from transifex");
				var streamM = await resultM.Content.ReadAsStreamAsync();

				var serializer = new JsonSerializer();

				TransifexLanguage[] languages;

				using (var sr = new StreamReader(streamM))
				using (var jsonTextReader = new JsonTextReader(sr))
				{
					languages = serializer.Deserialize<TransifexLanguage[]>(jsonTextReader);
				}

				var projectPath = Path.Combine(languageBasePath, project);
				Directory.CreateDirectory(projectPath);

				await Task.WhenAll(languages.Select(async lang =>
				{
					var language = lang.language_code;
					var request = TransifexRequest(HttpMethod.Get, $"https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/{language}/?file", transifex);

					var result = await client.SendAsync(request);
					if (!result.IsSuccessStatusCode)
						return;

					try { language = CultureInfo.GetCultureInfo(language.Replace("_", "-")).Name; }
					catch { return; }

					var languagePath = Path.Combine(projectPath, language);
					Directory.CreateDirectory(languagePath);

					using (var demoDataStream = System.IO.File.Open(Path.Combine(languagePath, "strings.resx"), FileMode.Create, FileAccess.Write))
					using (var stream = await result.Content.ReadAsStreamAsync())
					{
						await stream.CopyToAsync(demoDataStream);
					}
				}).ToArray());
			}

			return RebuildLanguageFiles(project);
		}

		[HttpPost("project/{project}/rebuild")]
		public IActionResult RebuildLanguageFiles(string project)
		{
			// GET https://www.transifex.com/api/2/project/ts3audiobot/languages
			// GET https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/en/?file

			var report = new List<BuildReport>();

			var projectPath = Path.Combine(languageBasePath, project);
			Directory.CreateDirectory(projectPath);

			db.LanguageTable.Delete(LiteDB.Query.All());

			foreach (var langFile in GetLanguageListDir(project))
			{
				string language;
				try { language = CultureInfo.GetCultureInfo(langFile).Name; }
				catch { continue; }

				var languagePath = Path.Combine(projectPath, language);
				if (!System.IO.File.Exists(Path.Combine(languagePath, "strings.resx")))
					continue;

				var result = ProcessFile(
					"resgen",
					"strings.resx",
					language, languagePath,
					"Failed to transform resx file");
				if (result != null) { report.Add(result); continue; }

				result = ProcessFile(
					"al",
					$"-target:lib -embed:strings.resources,TS3AudioBot.Localization.strings.{language}.resources -culture:{language} -out:TS3AudioBot.resources.dll",
					language, languagePath,
					"Failed to compile satellite assembly");
				if (result != null) { report.Add(result); continue; }

				System.IO.File.Copy(Path.Combine(languagePath, "TS3AudioBot.resources.dll"), Path.Combine(languagePath, "strings.dll"), true);

				var entryId = ToId(project, language);
				var langEntry = db.LanguageTable.FindById(entryId);
				if (langEntry == null)
				{
					langEntry = new LanguageEntry
					{
						Id = entryId,
						Language = language,
						Project = project,
					};
				}

				langEntry.UploadTime = DateTime.UtcNow;
				db.LanguageTable.Upsert(langEntry);

				report.Add(new BuildReport
				{
					language = language,
					ok = true,
				});
			}

			return Ok(report);
		}

		private static BuildReport ProcessFile(string bin, string arg, string language, string languagePath, string errMsg)
		{
			using (var proc = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = bin,
					Arguments = arg,
					WorkingDirectory = languagePath,
				}
			})
			{
				proc.Start();
				proc.WaitForExit(10000);

				if (proc.ExitCode != 0)
				{
					return new BuildReport
					{
						language = language,
						ok = false,
						message = errMsg
					};
				}
				return null;
			}
		}

		private static HttpRequestMessage TransifexRequest(HttpMethod method, string link, string token)
		{
			var request = new HttpRequestMessage(method, link);
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{token}")));
			return request;
		}

		private LanguageEntry GetLanguageEntry(string project, CultureInfo culture)
		{
			var id = ToId(project, culture.Name);
			return db.LanguageTable.FindById(id);
		}

		public static string ToId(string project, string language) => $"{project}.{language}";
	}

#pragma warning disable IDE1006 // Naming Styles
	internal class BuildReport
	{
		public string language { get; set; }
		public bool ok { get; set; }
		public string message { get; set; }
	}

	internal class TransifexLanguage
	{
		//public object coordinators { get; set; }
		public string language_code { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}
