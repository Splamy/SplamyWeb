using Newtonsoft.Json;
using SplamyWeb.Controllers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class TeamspeakService
	{
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private static readonly string[] CheckedNicknames = new[] { "loc", "splamy" };
		private readonly IHttpClientFactory clientFactory;

		public TeamspeakService(IHttpClientFactory clientFactory, TimerService timer)
		{
			timer.Register(UpdateVersionsAsync);
			timer.Register(UpdateBadgesAsync);
			timer.Register(KeepNicknamesValidAsync);
			this.clientFactory = clientFactory;
		}

		private async Task UpdateVersionsAsync()
		{
			try
			{
				using var client = clientFactory.CreateClient();
				var response = await client.GetAsync("https://ts3index.com/api/clientversions.php?id=LsnlCausp");
				var stream = await response.Content.ReadAsStreamAsync();

				JsonData? data;
				var serializer = new JsonSerializer();
				using (var sr = new StreamReader(stream))
				using (var jsonTextReader = new JsonTextReader(sr))
				{
					data = serializer.Deserialize<JsonData>(jsonTextReader);
				}

				if (data?.data is null || !data.success)
					return;

				var vsign = data.data.Select(x => new VersionSign(x.version, x.platform, x.sign)).ToArray();
				await TeamspeakController.TryAddNewVersionSignChecked(vsign);
			}
			catch (Exception ex) { Log.Warn(ex, "Failed to check verions: {0}", ex.Message); }
		}

		private async Task UpdateBadgesAsync()
		{
			try
			{
				using var client = clientFactory.CreateClient();
				var request = new HttpRequestMessage()
				{
					RequestUri = new Uri("https://badges-content.teamspeak.com/list"),
					Method = HttpMethod.Get,
				};
				request.Headers.UserAgent.Clear();
				request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:67.0) Gecko/20100101 Firefox/67.0");
				var cook = request.Headers.GetCookies();
				cook.Add(new CookieHeaderValue("__cfduid", "d10e713663dd1405a7d4055a1cb37436c1560562132"));
				cook.Add(new CookieHeaderValue("bb_lastvisit", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)));
				cook.Add(new CookieHeaderValue("bb_lastactivity", "0"));
				var response = await client.SendAsync(request);
				var stream = await response.Content.ReadAsStreamAsync();

				var badges = ProtoBuf.Serializer.Deserialize<Badges>(stream);

				if (badges?.BadgeList == null)
					return;

				TeamspeakController.AddNewBadge(badges);
			}
			catch (Exception ex) { Log.Warn(ex, "Failed to update badges: {0}", ex.Message); }
		}

		private async Task KeepNicknamesValidAsync()
		{
			using var client = clientFactory.CreateClient();

			foreach (var name in CheckedNicknames)
			{
				try
				{
					await client.GetAsync("https://named.myteamspeak.com/lookup?name=" + name);
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
}
