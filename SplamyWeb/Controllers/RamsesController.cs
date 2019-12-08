using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RateMapSeveritySaber;
using SplamyWeb.Components;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	public class RamsesController : Controller
	{
		private readonly BufferBlock<(string, TaskCompletionSource<RamsesEntry?>)> _bufferBlock = new BufferBlock<(string, TaskCompletionSource<RamsesEntry?>)>();
		private readonly LocalDb db;

		private readonly string RamsesVersion;

		public RamsesController(LocalDb db)
		{
			this.db = db;
			var ver = typeof(Analyzer).Assembly.GetName().Version!;
			RamsesVersion = $"{ver.Major}.{ver.Minor}";
			Process();
		}

		private async void Process()
		{
			while (true)
			{
				var (key, response) = await _bufferBlock.ReceiveAsync().ConfigureAwait(false);
				RamsesEntry? res;
				try
				{
					res = await GetInternal(key);
				}
				catch
				{
					res = new RamsesEntry
					{
						Id = key,
						Version = RamsesVersion,
						Maps = Array.Empty<RamsesMap>(),
					};
				}
				response.SetResult(res);
			}
		}

		[HttpGet("{key}")]
		public async Task<IActionResult> Index(string key)
		{
			var entry = await Get(key);
			if (entry is null)
				return this.Problem("Error Processing");
			return Ok(entry);
		}

		private async Task<RamsesEntry?> Get(string key)
		{
			var tcs = new TaskCompletionSource<RamsesEntry?>();
			await _bufferBlock.SendAsync((key, tcs));
			return await tcs.Task;
		}

		private async Task<RamsesEntry?> GetInternal(string key)
		{
			var entry = db.RamsesTable.FindById(key);
			if (entry != null && entry.Version == RamsesVersion)
				return entry;

			using var client = HttpClientFactory.Create();
			var data = await client.GetAsync($"https://beatsaver.com/api/download/key/{key}");
			var zip = new ZipArchive(await data.Content.ReadAsStreamAsync());
			var maps = BSMapIO.Read(file =>
			{
				var infoE = zip.GetEntry(file);
				return infoE.Open();
			});

			var parsedMaps = maps.Select(map =>
			{
				Score score;
				try
				{
					score = Analyzer.AnalyzeMap(map);
				}
				catch
				{
					score = new Score
					{
						Avg = -1,
						Max = -1,
						Graph = Array.Empty<float>(),
					};
				}

				return new RamsesMap
				{
					AvgDifficulty = score.Avg,
					MaxDifficulty = score.Max,
					Graph = score.Graph,
					Difficulty = map.MapInfo._difficulty,
				};
			}).ToArray();

			entry = new RamsesEntry
			{
				Id = key,
				Maps = parsedMaps,
				Version = RamsesVersion,
			};

			db.RamsesTable.Upsert(entry);

			return entry;
		}
	}

#pragma warning disable CS8618
	public class RamsesEntry
	{
		[JsonIgnore]
		public string Id { get; set; }
		[JsonProperty(PropertyName = "ramsesVersion")]
		public string Version { get; set; }
		[JsonProperty(PropertyName = "maps")]
		public RamsesMap[] Maps { get; set; }
	}

	public class RamsesMap
	{
		[JsonProperty(PropertyName = "difficulty")]
		public string Difficulty { get; set; }
		[JsonProperty(PropertyName = "maxDifficulty")]
		public float MaxDifficulty { get; set; }
		[JsonProperty(PropertyName = "avgDifficulty")]
		public float AvgDifficulty { get; set; }
		[JsonProperty(PropertyName = "graph")]
		public float[] Graph { get; set; }
	}
#pragma warning restore CS8618
}
