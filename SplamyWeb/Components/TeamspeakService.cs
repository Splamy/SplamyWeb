using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class TeamspeakService
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private static readonly string[] CheckedNicknames = new[] { "loc", "splamy" };

		private static readonly Regex diffMatch = new Regex("^diff --git (.*)$", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private static readonly Regex versionClean = new Regex(@"[^a-zA-Z0-9\+=/]");
		public static readonly byte[] Ts3VerionSignPublicKey = Convert.FromBase64String("UrN1jX0dBE1vulTNLCoYwrVpfITyo+NBuq/twbf9hLw=");

		private const string ProjectUrlBase = "https://api.github.com/repos/ReSpeak/tsdeclarations";
		private const string CsvHeader = "version,platform,hash\n";

		private readonly object cacheLock = new object();
		private HashSet<VersionSign> cachedVersions = new HashSet<VersionSign>();
		private string? cachedFileSha;
		private long LastBadgeUpdate = 0;
		private readonly CsvConfiguration CsvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);

		private readonly StoreService store;

		public TeamspeakService(TimerService timer, StoreService store)
		{
			timer.Register(UpdateVersionsAsync);
			timer.Register(UpdateBadgesAsync);
			timer.Register(KeepNicknamesValidAsync);
			this.store = store;
		}

		public static async Task<(bool safe, bool affectsVersion)> CheckSafeToAccept(string url)
		{
			try
			{
				bool safe = true;
				bool affectsVersion = false;

				using var response = await Util.httpClient.GetAsync(url);
				var diff = await response.Content.ReadAsStringAsync();
				foreach (var item in (IEnumerable<Match>)diffMatch.Matches(diff))
				{
					if (item.Value == "diff --git a/Version.csv b/Version.csv")
					{
						affectsVersion = true;
					}
					else
					{
						safe = false;
					}
				}
				return (safe, affectsVersion);
			}
			catch { return (false, false); }
		}

		public async Task TryAddNewVersionSignChecked(params VersionSign[] vsign)
		{
			var correctSigns = vsign.Select(x => CheckVersionClean(x)).Where(x => x != null).ToArray();
			await TryAddNewVersionSign(correctSigns!);
		}

		public async Task<IActionResult> TryAddNewVersionSign(params VersionSign[] vsign)
		{
			for (int i = 0; i < 4; i++)
			{
				var result = await AddNewVersionSign(vsign);
				if (result != null)
					return result;
				await Task.Delay(1000);
			}

			Log.Warn("Failed to push to github after multiple retries");
			return new ObjectResult("Github request could not be completed") { StatusCode = 503 };
		}

		private async Task<IActionResult?> AddNewVersionSign(params VersionSign[] vsign)
		{
			if (vsign.Length == 0)
				return new OkObjectResult("No signs requested");

			if (vsign.All(x => cachedVersions.Contains(x)))
				return new OkObjectResult("All signs ok. No new entries.");

			var file = await DownloadJson<Json_File>("/contents/Versions.csv");
			if (file == null) return new BadRequestObjectResult("No file found");

			HashSet<VersionSign> versions;
			bool recalculate;

			lock (cacheLock)
			{
				if (file.sha == cachedFileSha)
				{
					versions = cachedVersions;
					recalculate = false;
				}
				else
				{
					versions = new HashSet<VersionSign>();
					recalculate = true;
				}
			}

			if (recalculate)
			{
				var content = file.ContentString();
				var errors = new List<VersionError>();
				CheckFile(content, versions, errors);

				if (errors.Count > 0)
					return new UnprocessableEntityObjectResult(errors);

				lock (cacheLock)
				{
					cachedVersions = versions;
					cachedFileSha = file.sha;
				}
			}

			var newEntries = vsign.Where(x => !versions.Contains(x)).ToArray();

			if (newEntries.Length == 0)
				return new OkObjectResult("All signs ok. No new entries.");

			var newContent = CsvHeader + string.Join("\n",
				versions
				.Concat(newEntries)
				.OrderBy(x => x.BuildNumber)
				.ThenBy(x => x.Platform)
				.Select(x => x.ToString()));
			var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(newContent));

			var strb = new StringBuilder();
			strb.AppendFormat(CultureInfo.InvariantCulture, "Added new version: {0},{1}", newEntries[0].Build, newEntries[0].Platform);
			if (newEntries.Length > 1)
			{
				strb.AppendFormat(CultureInfo.InvariantCulture, " (and {0} more)", newEntries.Length - 1);
				strb.Append("\n\n");
				foreach (var newEntry in newEntries.Skip(1))
					strb.AppendFormat(CultureInfo.InvariantCulture, "New: {0},{1}\n", newEntry.Build, newEntry.Platform);
				strb.Length--;
			}

			bool postResult = await PutJson("/contents/Versions.csv", new Json_File_POST
			{
				branch = "master",
				content = base64Content,
				message = strb.ToString(),
				sha = file.sha,
			});

			if (!postResult)
				return null; /* Retry */

			foreach (var newSign in newEntries)
				Log.Info("Added new version: {0},{1}", newSign.Build, newSign.Platform);

			return new OkObjectResult("All signs ok. Added new ones to db.");
		}

		private static void CheckFile(string data, HashSet<VersionSign> duplicates, List<VersionError> errors)
		{
			if (data.Contains('\r', StringComparison.Ordinal))
			{
				errors.Add(new VersionError(-1, "File is not using consistent \\n line endigns."));
				data = data.Replace("\r", "", StringComparison.Ordinal);
			}

			var lines = data.Split('\n');
			if (string.IsNullOrEmpty(lines.Last()))
			{
				lines = lines.Take(lines.Length - 1).ToArray();
			}

			var header = lines[0].Split(',');
			int iname = Array.IndexOf(header, "version");
			int iplat = Array.IndexOf(header, "platform");
			int ihash = Array.IndexOf(header, "hash");

			foreach (var (line, lineNumber) in lines.Select((x, i) => (x, i)).Skip(1))
			{
				try
				{
					var split = line.Split(',');

					string name = split[iname];
					string platform = split[iplat];
					string hash = split[ihash];

					var vsign = new VersionSign(name, platform, hash);

					if (duplicates.Contains(vsign))
					{
						errors.Add(new VersionError(lineNumber, "Duplicate Entry", vsign));
						continue;
					}
					duplicates.Add(vsign);

					var checkResult = CheckVersion(vsign);
					if (checkResult != null)
					{
						checkResult.Line = lineNumber;
						errors.Add(checkResult);
					}
				}
				catch (Exception ex)
				{
					errors.Add(new VersionError(lineNumber, $"Invalid line ({ex.Message})"));
				}
			}
		}

		public static VersionSign? CheckVersionClean(VersionSign sign)
		{
			var checkResult = CheckVersion(sign);
			if (checkResult != null)
			{
				if (checkResult.FixedVersion != null)
					return checkResult.FixedVersion;
				else
					return null;
			}
			return sign;
		}

		public static VersionError? CheckVersion(VersionSign sign)
		{
			var tryFixSignStr = versionClean.Replace(sign.Sign, "");
			if (tryFixSignStr != sign.Sign)
			{
				var tryFixSign = new VersionSign(sign.Build, sign.Platform, tryFixSignStr);
				var result = EdCheck(tryFixSign);
				return result ?? new VersionError(-1, "The sign is correct but some junk characters were removed", sign) { FixedVersion = tryFixSign };
			}
			else
			{
				return EdCheck(sign);
			}
		}

		public static VersionError? EdCheck(VersionSign sign)
		{
			try
			{
				var ver = Encoding.ASCII.GetBytes(sign.Platform + sign.Build);
				if (!Chaos.NaCl.Ed25519.Verify(Convert.FromBase64String(sign.Sign), ver, Ts3VerionSignPublicKey))
					return new VersionError(-1, "Sign invalid", sign);
				return null;
			}
			catch (Exception ex) { return new VersionError(-1, $"Invalid data ({ex.Message})", sign); }
		}

		public async Task<IActionResult?> AddNewBadge(Badges badges)
		{
			if (badges.BadgeList.Length == 0)
				return new OkObjectResult("No badges requested");

			if (LastBadgeUpdate == badges.LastUpdate)
				return new OkObjectResult("Badge file hasn't changed");

			var file = await DownloadJson<Json_File>("/contents/Badges.csv");
			if (file == null) return new BadRequestObjectResult("No file found");

			var dictBadges = new Dictionary<string, CSV_Badge>();
			using (var csvReader = new CsvReader(new StreamReader(file.ContentStream()), CsvConfig))
				foreach (var badge in csvReader.GetRecords<CSV_Badge>())
					dictBadges[badge.uid] = badge;

			bool changed = false;

			var newEntries = new List<CSV_Badge>();

			foreach (var badge in badges.BadgeList)
			{
				// 0 - empty, 1 - guid, 2 - filename
				var filename = new Uri(badge.Url).AbsolutePath.Split('/')[2];

				if (dictBadges.TryGetValue(badge.Guid, out var compareBadge))
				{
					if (compareBadge.name != badge.Name) { compareBadge.name = badge.Name; changed = true; }
					if (compareBadge.description != badge.Description) { compareBadge.description = badge.Description; changed = true; }
					if (compareBadge.filename != filename) { compareBadge.filename = filename; changed = true; }
				}
				else
				{
					changed = true;

					dictBadges[badge.Guid] = compareBadge = new CSV_Badge()
					{
						uid = badge.Guid,
						name = badge.Name,
						description = badge.Description,
						filename = filename,
						codes = "",
					};
					newEntries.Add(compareBadge);
				}
			}

			if (changed)
			{
				string base64Content;
				using (var mem = new MemoryStream())
				using (var sw = new StreamWriter(mem))
				using (var csvWriter = new CsvWriter(sw, CsvConfig))
				{
					var badgeSort = dictBadges.Values.ToList();
					badgeSort.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
					csvWriter.WriteRecords(badgeSort);
					csvWriter.Flush();
					sw.Flush();
					mem.Seek(0, SeekOrigin.Begin);
					base64Content = Convert.ToBase64String(mem.ToArray());
				}

				bool postResult = await PutJson("/contents/Badges.csv", new Json_File_POST
				{
					branch = "master",
					content = base64Content,
					message = "Added new badge\n\n" + string.Join("\n", newEntries.Select(x => $"New: {x.uid},{x.name}")),
					sha = file.sha,
				});

				if (!postResult)
					return null; /* Retry */
			}

			foreach (var badge in newEntries)
				Log.Info("Added new badge: {0},{1}", badge.uid, badge.name);

			LastBadgeUpdate = badges.LastUpdate;

			return new OkObjectResult("All signs ok. Added new ones to db.");
		}

		private static async Task<T?> DownloadJson<T>(string action) where T : class
		{
			try
			{
				using var response = await Util.httpClient.GetAsync(ProjectUrlBase + action);
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadFromJsonAsync<T>(Util.JsonDefault);
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				return null;
			}
		}

		private async Task<bool> PutJson<T>(string action, T data) where T : class
		{
			try
			{

				var json = JsonSerializer.Serialize(data, Util.JsonDefault);
				var content = new StringContent(json, Encoding.UTF8, "application/json");
				var request = new HttpRequestMessage(HttpMethod.Put, ProjectUrlBase + action)
				{
					Content = content,
				};
				request.Headers.Authorization = new AuthenticationHeaderValue("Basic", await store.GetGithubAuth());
				using var response = await Util.httpClient.SendAsync(request);
				response.EnsureSuccessStatusCode();
				return true;
			}
			catch (HttpRequestException ex)
			{
				Log.Warn(ex, "Error uploading to github: " + ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error accessing github");
				return false;
			}
		}

		// HServices

		private async Task UpdateVersionsAsync()
		{
			try
			{
				using var response = await Util.httpClient.GetAsync("https://ts3index.com/api/clientversions.php?id=LsnlCausp");
				JsonData? data = await response.Content.ReadFromJsonAsync<JsonData?>(Util.JsonDefault);

				if (data?.data is null || !data.success)
					return;

				var vsign = data.data.Select(x => new VersionSign(x.version, x.platform, x.sign)).ToArray();
				await TryAddNewVersionSignChecked(vsign);
			}
			catch (Exception ex) { Log.Warn(ex, "Failed to check verions: {0}", ex.Message); }
		}

		private async Task UpdateBadgesAsync()
		{
			try
			{
				var request = new HttpRequestMessage(HttpMethod.Get, "https://badges-content.teamspeak.com/list");
				request.Headers.UserAgent.Clear();
				request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:67.0) Gecko/20100101 Firefox/67.0");
				var cook = request.Headers.GetCookies();
				cook.Add(new CookieHeaderValue("__cfduid", "d10e713663dd1405a7d4055a1cb37436c1560562132"));
				cook.Add(new CookieHeaderValue("bb_lastvisit", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
				cook.Add(new CookieHeaderValue("bb_lastactivity", "0"));
				using var response = await Util.httpClient.SendAsync(request);
				using var stream = await response.Content.ReadAsStreamAsync();

				var badges = Serializer.Deserialize<Badges>(stream);

				if (badges?.BadgeList == null)
					return;

				await AddNewBadge(badges);
			}
			catch (Exception ex) { Log.Warn(ex, "Failed to update badges: {0}", ex.Message); }
		}

		private async Task KeepNicknamesValidAsync()
		{
			foreach (var name in CheckedNicknames)
			{
				try
				{
					using var _ = await Util.httpClient.GetAsync("https://named.myteamspeak.com/lookup?name=" + name);
				}
				catch (Exception ex) { Log.Warn(ex, "Failed to check nickname: {0}", name); }
			}
		}

#pragma warning disable CS8618, IDE1006
		public class JsonData
		{
			public bool success { get; set; }
			public JsonVersion[]? data { get; set; }
		}

		public class JsonVersion
		{
			public string platform { get; set; }
			public string version { get; set; }
			public string sign { get; set; }
#pragma warning restore CS8618, IDE1006
		}
	}

	public class VersionError
	{
		public int Line { get; set; }
		public string Error { get; }
		public VersionSign? Version { get; }
		public VersionSign? FixedVersion { get; set; }

		public VersionError(int line, string error, VersionSign? version = null)
		{
			FixedVersion = null;
			Line = line;
			Error = error;
			Version = version;
		}
	}

	public sealed class VersionSign : IEquatable<VersionSign>
	{
		public string Sign { get; }
		public string Build { get; }
		public long BuildNumber { get; }
		public string Platform { get; }

		private static readonly Regex buildMatch = new Regex(@"\[Build: (\d+)\]", RegexOptions.Compiled | RegexOptions.ECMAScript);

		public VersionSign(string build, string platform, string sign)
		{
			Build = build ?? throw new ArgumentNullException(nameof(build));
			Platform = platform ?? throw new ArgumentNullException(nameof(platform));
			Sign = sign ?? throw new ArgumentNullException(nameof(sign));

			var match = buildMatch.Match(Build);
			if (match.Success && long.TryParse(match.Groups[1].Value, out var buildNum))
				BuildNumber = buildNum;
			else
				BuildNumber = -1;
		}

		public override bool Equals(object? obj) => Equals(obj as VersionSign);

		public bool Equals(VersionSign? other)
			=> other != null
			&& Sign == other.Sign
			&& Build == other.Build
			&& Platform == other.Platform;

		public override int GetHashCode() => HashCode.Combine(Sign, Build, Platform);

		public override string ToString() => $"{Build},{Platform},{Sign}";
	}

#pragma warning disable CS8618, IDE1006
	[ProtoContract]
	public class Badges
	{
		[ProtoMember(1)]
		public long _1 { get; set; }
		[ProtoMember(2)]
		public long LastUpdate { get; set; }
		[ProtoMember(3)]
		public Badge[] BadgeList { get; set; }
	}

	[ProtoContract]
	public class Badge
	{
		[ProtoMember(1)]
		public string Guid { get; set; }
		[ProtoMember(2)]
		public string Name { get; set; }
		[ProtoMember(3)]
		public string Url { get; set; }
		[ProtoMember(4)]
		public string Description { get; set; }
		[ProtoMember(5)]
		public long Timestamp { get; set; }
		[ProtoMember(6)]
		public long _1 { get; set; }
	}

	public class CSV_Badge
	{
		public string uid { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public string filename { get; set; }
		public string codes { get; set; }
	}

	public class Json_Github
	{
		public string action { get; set; }
		public Json_PullRequest pull_request { get; set; }
		public Json_Repository repository { get; set; }
	}

	public class Json_PullRequest
	{
		public string diff_url { get; set; }
		public string state { get; set; }
		public int number { get; set; }
		public Json_Commit head { get; set; }
		public Json_Commit @base { get; set; }
	}

	public class Json_Commit
	{
		public string label { get; set; }
		public string @ref { get; set; }
		public string sha { get; set; }
		public Json_Repository repo { get; set; }
	}

	public class Json_Repository
	{
		public string name { get; set; }
		public string full_name { get; set; }
	}

	public class Json_Comment
	{
		public string body { get; set; }
	}

	public class Json_File
	{
		public string name { get; set; }
		public string path { get; set; }
		public string sha { get; set; }
		public string content { get; set; }
		public string encoding { get; set; }

		public Stream ContentStream() => new MemoryStream(Convert.FromBase64String(content));
		public string ContentString() => Encoding.UTF8.GetString(Convert.FromBase64String(content));
	}

	public class Json_File_POST
	{
		public string message { get; set; }
		public string content { get; set; }
		public string sha { get; set; }
		public string branch { get; set; }
	}
#pragma warning restore CS8618, IDE1006

}
