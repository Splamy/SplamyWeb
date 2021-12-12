using AutoMapper;
using AutoMapper.QueryableExtensions;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = Util.AuthScheme)]
[Route("api/[controller]")]
public class LanguageController : ControllerBase
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
	private static readonly string languageBasePath = Path.Combine(Util.DataPath, "language");

	private readonly UserManager<LoginData> userManager;
	private readonly SplamyContext db;
	private readonly StoreService store;

	public LanguageController(SplamyContext db, StoreService store, UserManager<LoginData> userManager)
	{
		this.db = db;
		this.store = store;
		this.userManager = userManager;
	}

	[AllowAnonymous]
	[HttpGet("project/{project}/languages")]
	[Produces(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> GetLanguageList(string project)
	{
		if (!IsSave(project))
			return BadRequest("Invalid project");

		project = project.ToLowerInvariant();

		var isAdmin = await ExtendedPermission();

		var result = await db.LanguageEntries
			.AsNoTracking()
			.Where(x => x.Project == project)
			.ProjectTo<LangInfo>(isAdmin ? AdminMapping : UserMapping)
			.ToListAsync();

		result.ForEach(x => x.DisplayName = CultureInfo.GetCultureInfo(x.Language).DisplayName);
		return new JsonResult(result, JsonWebHideNull);
	}

	[AllowAnonymous]
	[HttpGet("project/{project}/language/{language}/dll")]
	[Produces(MediaTypeNames.Application.Octet, MediaTypeNames.Text.Plain)]
	public async Task<IActionResult> GetLanguageFile(string project, string language)
	{
		if (!IsSave(project) || !IsSave(language))
			return BadRequest("Invalid project or language");

		project = project.ToLowerInvariant();
		CultureInfo culture;
		try { culture = CultureInfo.GetCultureInfo(language); }
		catch { return NotFound("Culture not found"); }

		var langEntry = await GetLanguageEntry(project, culture);
		// TODO change when other projects are added
		var fullPath = new FileInfo(Path.Combine(languageBasePath, project, culture.Name, "TS3AudioBot.resources.dll"));
		if (langEntry == null || !fullPath.Exists)
			return NotFound("The language was not found");

		langEntry.DownloadCount++;
		await db.SaveChangesAsync();

		return PhysicalFile(fullPath.FullName, MediaTypeNames.Application.Octet, "strings.dll");
	}

	[HttpPost("project/{project}/update")]
	public Task<IActionResult> UpdateLanguageFilesAsync(string project) => RebuildInternal(project, downloadFiles: true);

	[HttpPost("project/{project}/rebuild")]
	public Task<IActionResult> RebuildLanguageFiles(string project) => RebuildInternal(project, downloadFiles: false);

	// TODO save download counts
	private async Task<IActionResult> RebuildInternal(string project, bool downloadFiles)
	{
		if (project != "ts3ab")
			return BadRequest("Project not supported");

		if (!IsSave(project))
			return BadRequest("Invalid project");

		// GET https://www.transifex.com/api/2/project/ts3audiobot/languages
		// GET https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/en/?file

		var projectData = await db.NightlyProjects.FindAsync(project);
		if (projectData is null)
			return BadRequest("Project not found");

		var projectPath = Path.Combine(languageBasePath, project);
		Directory.CreateDirectory(projectPath);

		if (downloadFiles)
		{
			Log.Info("Requested language update");
			using var requestM = await TransifexRequest(HttpMethod.Get, "https://www.transifex.com/api/2/project/ts3audiobot/languages");
			using var resultM = await httpClient.SendAsync(requestM);
			if (!resultM.IsSuccessStatusCode)
				return UnprocessableEntity("Error from transifex");
			var languages = await resultM.Content.ReadFromJsonAsync<TransifexLanguage[]>(JsonDefault);
			if (languages is null)
				return Problem("Could not get languages");

			await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE nightly_lang");

			Log.Info("Fetching all localization files from transifex");
			await Task.WhenAll(languages.Select(async lang =>
			{
				string language;
				try
				{
					language = CultureInfo.GetCultureInfo(lang.language_code.Replace("_", "-", StringComparison.Ordinal)).Name;
					if (string.Equals(language, "BS-BA", StringComparison.OrdinalIgnoreCase))
						language = "bs";
				}
				catch { return; }

				using var request = await TransifexRequest(HttpMethod.Get, $"https://www.transifex.com/api/2/project/ts3audiobot/resource/stringsresx/translation/{lang.language_code}/?file");
				using var result = await httpClient.SendAsync(request);
				if (!result.IsSuccessStatusCode)
					return;

				using var demoDataStream = System.IO.File.Open(Path.Combine(projectPath, $"strings.{language}.resx"), FileMode.Create, FileAccess.Write);
				using var stream = await result.Content.ReadAsStreamAsync();
				await stream.CopyToAsync(demoDataStream);

				await db.LanguageEntries.AddAsync(new LanguageEntry
				{
					Language = language,
					Project = project,
					UploadTime = DateTime.UtcNow
				});
			}));

			await db.SaveChangesAsync();
		}

		Log.Info("Requested language rebuild");

		var buildCsproj = await store.Get($"lang_build_{project}");
		if (buildCsproj is null)
			return StatusCode((int)HttpStatusCode.InternalServerError, "No build config for nighly data");

		var csprojFile = Path.Combine(projectPath, "build.csproj");
		if (System.IO.File.Exists(csprojFile))
			System.IO.File.Delete(csprojFile);
		System.IO.File.WriteAllText(csprojFile, buildCsproj, Encoding.UTF8);

		BufferedCommandResult buildRes;
		try
		{
			// TODO 10 sec timout
			buildRes = await Cli.Wrap("dotnet")
				.WithArguments("build -c Release")
				.WithWorkingDirectory(projectPath)
				.WithValidation(CommandResultValidation.None)
				.ExecuteBufferedAsync();

			if (buildRes.ExitCode != 0)
			{
				return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to build: " + buildRes.StandardOutput);
			}
		}
		catch (Exception ex)
		{
			return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to build (process error): " + ex.Message);
		}

		var languageEntries = await db.LanguageEntries.ToArrayAsync();
		foreach (var dir in languageEntries)
		{
			if (!Directory.Exists(Path.Combine(projectPath, dir.Language)))
			{
				Log.Warn("Unknown language: {0}", dir.Language);
				db.LanguageEntries.Remove(dir);
			}
		}

		await db.SaveChangesAsync();
		return Ok(buildRes.StandardOutput);
	}

	/*
	 * Example:
<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup>
	<TargetFramework>netstandard2.0</TargetFramework>
	<RootNamespace>TS3AudioBot.Localization</RootNamespace>
	<AssemblyName>TS3AudioBot</AssemblyName>
	<OutputPath>.</OutputPath>
	<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
	<AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
</PropertyGroup>
</Project>
	 */

	private async ValueTask<HttpRequestMessage> TransifexRequest(HttpMethod method, string link)
	{
		var request = new HttpRequestMessage(method, link);
		var auth = await store.GetTransifexAuth();
		request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
			Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{auth}")));
		return request;
	}

	private async Task<LanguageEntry?> GetLanguageEntry(string project, CultureInfo culture)
		=> await db.LanguageEntries.FindAsync(project, culture.Name);

	private async Task<bool> ExtendedPermission()
	{
		var user = await userManager.GetUserAsync(User);
		if (user is null)
			return false;

		return user.Rank.AtLeast(UserType.Admin);
	}

	static readonly MapperConfiguration AdminMapping = new(cfg =>
	{
		cfg.CreateMap<LanguageEntry, LangInfo>(MemberList.None);
	});

	static readonly MapperConfiguration UserMapping = new(cfg =>
	{
		cfg.CreateMap<LanguageEntry, LangInfo>(MemberList.None)
			.ForMember(x => x.DownloadCount, opt => opt.MapFrom((_) => (int?)null));
	});
}

#pragma warning disable IDE1006, CS8618 // Naming Styles
internal class TransifexLanguage
{
	//public object coordinators { get; set; }
	public string language_code { get; set; }
}

class LangInfo
{
	public string Project { get; set; }
	public string Language { get; set; }
	public DateTime UploadTime { get; set; }
	public string DisplayName { get; set; }
	public int? DownloadCount { get; set; }
}
