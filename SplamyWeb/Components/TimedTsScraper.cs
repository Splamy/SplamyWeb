using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using SplamyWeb.Controllers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	internal class TimedTsScraper : IHostedService, IDisposable
	{
		private readonly IHttpClientFactory _clientFactory;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private Timer timer;

		public TimedTsScraper(IHttpClientFactory clientFactory)
		{
			_clientFactory = clientFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Log.Info("TS3Index scraper Service is starting.");

			timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(1));

			return Task.CompletedTask;
		}

		private async void DoWork(object state)
		{
			Log.Info("Started scrape");

			await UpdateVersions().ConfigureAwait(false);
			await UpdateBadges().ConfigureAwait(false);

			Log.Info("Done scape");
		}

		private async Task UpdateVersions()
		{
			try
			{
				using var client = _clientFactory.CreateClient();
				var response = await client.GetAsync("https://ts3index.com/api/clientversions.php?id=LsnlCausp").ConfigureAwait(false);
				var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

				JsonData data;
				var serializer = new JsonSerializer();
				using (var sr = new StreamReader(stream))
				using (var jsonTextReader = new JsonTextReader(sr))
				{
					data = serializer.Deserialize<JsonData>(jsonTextReader);
				}

				if (!data.success || data.data == null)
					return;

				var vsign = data.data.Select(x => new VersionSign(x.version, x.platform, x.sign)).ToArray();
				await TeamspeakController.TryAddNewVersionSignChecked(vsign).ConfigureAwait(false);
			}
			catch (Exception ex) { Log.Warn("Failed to check verions: {0}", ex.Message); }
		}

		private async Task UpdateBadges()
		{
			try
			{
				using var client = _clientFactory.CreateClient();
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
			catch (Exception ex) { Log.Warn("Failed to update badges: {0}", ex.Message); }
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			Log.Info("TS3Index scraper Service is stopping.");

			timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			timer?.Dispose();
		}

		class JsonData
		{
			public bool success { get; set; }
			public JsonVersion[] data { get; set; }
		}

		class JsonVersion
		{
			public string platform { get; set; }
			public string version { get; set; }
			public string sign { get; set; }
		}
	}
}
