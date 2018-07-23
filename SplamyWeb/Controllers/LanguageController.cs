using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
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
		private readonly string languagePath = Path.Combine(LocalDb.DataPath, "language");

		[HttpGet("project/{project}/languages")]
		[Produces(MediaTypeNames.Application.Json)]
		public IActionResult GetLanguageList(string project)
		{
			if (!Save(project))
				return BadRequest("Invalid path");

			project = project.ToLower();

			var fullPath = new DirectoryInfo(Path.Combine(languagePath, project));
			if (!fullPath.Exists)
				return NotFound("The language was not found");

			return Ok(fullPath.GetDirectories().Select(d => d.Name));
		}

		[HttpGet("project/{project}/language/{language}/dll")]
		[Produces(MediaTypeNames.Application.Octet)]
		public IActionResult GetLanguageFile(string project, string language)
		{
			if (!Save(project) || !Save(language))
				return BadRequest("Invalid path");

			project = project.ToLower();
			language = language.ToLower();

			var langEntry = GetLanguageEntry(project, language);
			var fullPath = new FileInfo(Path.Combine(languagePath, project, language, "strings.dll"));
			if (langEntry == null || !fullPath.Exists)
				return NotFound("The language was not found");

			langEntry.DownloadCount++;
			LocalDb.LanguageTable.Upsert(langEntry);

			return PhysicalFile(fullPath.FullName, MediaTypeNames.Application.Octet, "strings.dll");
		}

		[HttpPost("project/{project}/update")]
		public async Task<IActionResult> UpdateLanguageFiles(string project, [FromQuery] string transifex)
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

				var projectPath = Path.Combine(languagePath, project);
				Directory.CreateDirectory(projectPath);

				await Task.WhenAll(languages.Select(async lang =>
				{
					var language = lang.language_code;
					var request = TransifexRequest(HttpMethod.Get, $"https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/{language}/?file", transifex);

					var result = await client.SendAsync(request);
					if (!result.IsSuccessStatusCode)
						return;

					var languagePath = Path.Combine(projectPath, language);
					Directory.CreateDirectory(languagePath);

					using (var demoDataStream = System.IO.File.OpenWrite(Path.Combine(languagePath, "strings.resx")))
					using (var stream = await result.Content.ReadAsStreamAsync())
					{
						await stream.CopyToAsync(demoDataStream);
					}

					using (var proc = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = "resgen",
							Arguments = "strings.resx",
							WorkingDirectory = languagePath,
						}
					})
					{
						proc.Start();
						proc.WaitForExit(10000);
					}

					using (var proc = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = "al",
							Arguments = $"-target:lib -embed:strings.resources -culture:{language} -out:strings.dll",
							WorkingDirectory = languagePath,
						}
					})
					{
						proc.Start();
						proc.WaitForExit(10000);
					}

					var entryId = ToId(project, language);
					var langEntry = LocalDb.LanguageTable.FindById(entryId);
					if (langEntry == null)
						langEntry = new LanguageEntry
						{
							Id = entryId,
							Language = language,
							Project = project,
						};

					langEntry.UploadTime = DateTime.UtcNow;
					LocalDb.LanguageTable.Upsert(langEntry);

				}).ToArray());
			}

			return Ok();
		}

		private static HttpRequestMessage TransifexRequest(HttpMethod method, string link, string token)
		{
			var request = new HttpRequestMessage(method, link);
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
				Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{token}")));
			return request;
		}

		private static LanguageEntry GetLanguageEntry(string project, string language)
		{
			var id = ToId(project, language);
			return LocalDb.LanguageTable.FindById(id);
		}

		public static string ToId(string project, string language) => $"{project}.{language}";
	}

	class TransifexLanguage
	{
		//public object coordinators { get; set; }
		public string language_code { get; set; }
	}
}
