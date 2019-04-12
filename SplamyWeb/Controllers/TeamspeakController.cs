using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SplamyWeb.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TeamspeakController : ControllerBase
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private static readonly WebClient wc = new WebClient();
		private static readonly Regex diffMatch = new Regex(@"^diff --git (.*)$", RegexOptions.Compiled | RegexOptions.ECMAScript);
		private const string ProjectUrlBase = "https://api.github.com/repos/ReSpeak/tsdeclarations";
		private const string CsvHeader = "version,platform,hash\n";
		private static readonly string AuthData = System.IO.File.ReadAllText(Path.Combine(LocalDb.DataPath, "github_auth"));

		private static readonly object cacheLock = new object();
		private static HashSet<VersionSign> cachedVersions = new HashSet<VersionSign>();
		private static string cachedFileSha;

		public static readonly byte[] Ts3VerionSignPublicKey = Convert.FromBase64String("UrN1jX0dBE1vulTNLCoYwrVpfITyo+NBuq/twbf9hLw=");

		[HttpPost("github")]
		[Consumes("application/json")] // , "text/csv"
		public IActionResult GithubPullRequest([FromBody] JObject data) // [FromQuery] string token, 
		{
			//var user = LocalDb.GetUserByToken(token);
			//if (user == null)
			//	return BadRequest("Not authorized");

			var json = data.ToObject<Json_Github>();

			if (json.pull_request != null)
			{
				if (json.pull_request.state == "closed")
					return Ok();

				var (safe, affectsVersion) = CheckSafeToAccept(json.pull_request.diff_url);

				Log.Debug("PR #{0} is safe:{1} version:{2}", json.pull_request.number, safe, affectsVersion);
			}

			Console.WriteLine("Action: {0}", json.action);

			return Ok();
		}

		private static (bool safe, bool affectsVersion) CheckSafeToAccept(string url)
		{
			try
			{
				bool safe = true;
				bool affectsVersion = false;

				var diff = wc.DownloadString(url);
				foreach (Match item in diffMatch.Matches(diff))
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

		//https://api.github.com/repos/Splamy/TravisExperiments/contents/ts3notify.sh

		// , "text/csv"
		[HttpPost("version/{build}/{platform}")]
		[Produces("application/json")]
		public async Task<IActionResult> AddNewVersionSign(string build, string platform, [FromQuery] string sign)
		{
			Log.Debug("Got version request for {0},{1}", build, platform);

			//var contentType = this.Request.ContentType;
			var vsign = new VersionSign(build, platform, sign);
			var checkResult = CheckVersion(vsign);
			if (checkResult != null)
			{
				if (checkResult.FixedVersion != null)
					vsign = checkResult.FixedVersion;
				else
					return UnprocessableEntity(checkResult);
			}

			for (int i = 0; i < 4; i++)
			{
				var result = TryAddNewVersionSign(vsign);
				if (result != null)
					return result;
				await Task.Delay(1000).ConfigureAwait(false);
			}

			Log.Warn("Failed to push to github after multiple retries");
			return StatusCode(503, "Github request could not be completed");
		}

		public IActionResult TryAddNewVersionSign(params VersionSign[] vsign)
		{
			if (vsign.Length == 0)
				return Ok("No signs requested");

			if (vsign.All(x => cachedVersions.Contains(x)))
				return Ok("All signs ok. No new entries.");

			var file = DownloadJson<Json_File>("/contents/Versions.csv");
			if (file == null) return BadRequest("No file found");

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
				var content = Encoding.UTF8.GetString(Convert.FromBase64String(file.content));
				var errors = new List<VersionError>();
				CheckFile(content, versions, errors);

				if (errors.Count > 0)
					return UnprocessableEntity(errors);

				lock (cacheLock)
				{
					cachedVersions = versions;
					cachedFileSha = file.sha;
				}
			}

			var newEntries = vsign.Where(x => !versions.Contains(x)).ToArray();

			if (newEntries.Length == 0)
				return Ok("All signs ok. No new entries.");

			var newContent = CsvHeader + string.Join("\n",
				versions
				.Concat(newEntries)
				.OrderBy(x => x.BuildNumber)
				.ThenBy(x => x.Platform)
				.Select(x => x.ToString()));
			var base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(newContent));

			bool postResult = PutJson("/contents/Versions.csv", new Json_File_POST
			{
				branch = "master",
				content = base64Content,
				message = "Added new version\n\n" + string.Join("\n", newEntries.Select(x => $"New: {x.Build},{x.Platform}")),
				sha = file.sha,
			});

			if (!postResult)
				return null; /* Retry */

			foreach (var newSign in newEntries)
				Log.Info("Added new version: {0},{1}", newSign.Build, newSign.Platform);

			return Ok("All signs ok. Added new ones to db.");
		}

		private static void CheckFile(string data, HashSet<VersionSign> duplicates, List<VersionError> errors)
		{
			if (data.Contains('\r'))
			{
				errors.Add(new VersionError(-1, "File is not using consistent \\n line endigns."));
				data = data.Replace("\r", "");
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

		public static VersionError CheckVersion(VersionSign sign)
		{
			if (sign.Sign.Contains('\\'))
			{
				var tryFixSign = new VersionSign(sign.Build, sign.Platform, sign.Sign.Replace("\\", ""));
				var result = EdCheck(tryFixSign);
				return result ?? new VersionError(-1, "The sign is correct but you forgot to remove all backslashes ('\\')", sign) { FixedVersion = tryFixSign };
			}

			return EdCheck(sign);
		}

		public static VersionError EdCheck(VersionSign sign)
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

		private static T DownloadJson<T>(string action) where T : class
		{
			try
			{
				var request = WebRequest.Create(ProjectUrlBase + action);
				request.Method = "GET";
				request.Headers[HttpRequestHeader.UserAgent] = "TAB Service Bot";
				using (var resp = request.GetResponse())
				using (var stream = resp.GetResponseStream())
				{
					if (stream == null)
						return null;

					using (var streamReader = new StreamReader(stream, Encoding.UTF8))
					using (var jsonReader = new JsonTextReader(streamReader))
					{
						var ser = new JsonSerializer();
						return ser.Deserialize<T>(jsonReader);
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex);
				return null;
			}
		}

		private static bool PutJson<T>(string action, T data) where T : class
		{
			try
			{
				var json = JsonConvert.SerializeObject(data);
				var jsonBytes = Encoding.UTF8.GetBytes(json);

				var request = WebRequest.Create(ProjectUrlBase + action);
				request.Method = "PUT";
				request.ContentType = "application/json";
				request.Headers[HttpRequestHeader.Authorization] = $"Basic {AuthData}";
				request.Headers[HttpRequestHeader.UserAgent] = "TAB Service Bot";
				request.ContentLength = jsonBytes.Length;

				using (var stream = request.GetRequestStream())
				{
					stream.Write(jsonBytes, 0, jsonBytes.Length);
				}

				using (var response = request.GetResponse()) { }

				return true;
			}
			catch (WebException ex)
			{
				var mem = new MemoryStream();
				ex.Response.GetResponseStream().CopyTo(mem);
				var str = Encoding.UTF8.GetString(mem.ToArray());
				Log.Warn(ex, "Error uploading to github: " + str);

				return false;
			}
			catch (Exception ex)
			{
				Log.Warn(ex, "Error accessing github");
				return false;
			}
		}
	}

	public class VersionError
	{
		public int Line { get; set; }
		public string Error { get; }
		public VersionSign Version { get; }
		public VersionSign FixedVersion { get; set; }

		public VersionError(int line, string error, VersionSign version = null)
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

		public override bool Equals(object obj) => Equals(obj as VersionSign);

		public bool Equals(VersionSign other)
			=> other != null
			&& Sign == other.Sign
			&& Build == other.Build
			&& Platform == other.Platform;

		public override int GetHashCode() => HashCode.Combine(Sign, Build, Platform);

		public override string ToString() => $"{Build},{Platform},{Sign}";
	}

#pragma warning disable IDE1006 // Naming Styles
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
	}

	public class Json_File_POST
	{
		public string message { get; set; }
		public string content { get; set; }
		public string sha { get; set; }
		public string branch { get; set; }
	}
#pragma warning restore IDE1006 // Naming Styles
}