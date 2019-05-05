using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SplamyWeb.Controllers;
using System.Net.Http;
using System.IO;

namespace SplamyWeb.Components
{
	internal class TimedTsScraper : IHostedService, IDisposable
	{
		private readonly IHttpClientFactory _clientFactory;
		static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private Timer timer;

		public TimedTsScraper(IHttpClientFactory clientFactory)
		{
			_clientFactory = clientFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			Log.Info("TS3Index scraper Service is starting.");

			timer = new Timer(DoWork, null, TimeSpan.Zero,
				TimeSpan.FromHours(1));

			return Task.CompletedTask;
		}

		private async void DoWork(object state)
		{
			try
			{
				Log.Info("Startet version check");

				var client = _clientFactory.CreateClient();
				var response = await client.GetAsync("https://ts3index.com/api/clientversions.php?id=LsnlCausp");
				var stream = await response.Content.ReadAsStreamAsync();

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
				await TeamspeakController.TryAddNewVersionSignChecked(vsign);

				Log.Info("Version check done");
			}
			catch (Exception ex) { Log.Warn("Failed to check verions: {0}", ex.Message); }
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
