using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TabController : ControllerBase
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	private readonly TabBackingData tab;
	private readonly SpamBackingData spam;

	public TabController(TabBackingData tab, SpamBackingData spam)
	{
		this.tab = tab;
		this.spam = spam;
	}

	[HttpPost("stats")]
	[Consumes("application/json")]
	public async Task PostPing(CancellationToken cancellationToken)
	{
		var remoteIp = Request.HttpContext.Connection.RemoteIpAddress;
		if (remoteIp is null || !spam.Check(remoteIp))
			return;

		const int MaxPingBodySize = 2_000;
		byte[] readBuffer = ArrayPool<byte>.Shared.Rent(MaxPingBodySize);
		Memory<byte> readSlice = readBuffer.AsMemory(..MaxPingBodySize);
		try
		{
			var read = await Request.Body.ReadAsync(readSlice, cancellationToken);
			readSlice = readSlice[..read];

			var obj = JsonSerializer.Deserialize<TabStatsData?>(readSlice.Span, Util.JsonDefault);

			if (obj != null)
				await tab.Add(obj);
		}
		catch (Exception jsonEx)
		{
			string jsonPeek = "";
			try
			{
				CleanNewlines(readSlice.Span);
				jsonPeek = Util.Utf8Encoding.GetString(readSlice.Span);
			}
			catch (Exception ex)
			{
				jsonPeek = ex.Message;
			}

			var path = jsonEx is JsonException jex ? jex.Path : null;
			Log.Debug("Ping Er: {0} {1} {2}", jsonEx.Message, path, jsonPeek);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(readBuffer);
		}
	}

	private static void CleanNewlines(Span<byte> data)
	{
		for (int i = 0; i < data.Length; i++)
			data[i] = data[i] is (byte)'\r' or (byte)'\n' ? (byte)' ' : data[i];
	}

	public static readonly string[] ImpMod = { "", "K", "M", "G" };
	public static string FormatMetric(uint number)
	{
		uint pow = number > 0 ? (uint)Math.Log10(number) : 0;
		string unit = ImpMod[pow / 3];
		double trimmedNumber = number / Math.Pow(1000, pow / 3);

		return $"{trimmedNumber:0.#}{unit}";
	}
	public static string FormatTime(TimeSpan time)
	{
		if (time < TimeSpan.FromMinutes(1))
			return $"{(int)time.TotalSeconds} sec";
		if (time < TimeSpan.FromHours(1))
			return $"{(int)time.TotalMinutes} min";
		if (time < TimeSpan.FromDays(1))
		{
			var h = (int)time.TotalHours;
			return $"{h} hour{(h > 1 ? "s" : "")}";
		}
		const double avgDaysPerYear = 365.2425;
		if (time < TimeSpan.FromDays(avgDaysPerYear))
		{
			var d = (int)time.TotalDays;
			return $"{d} day{(d > 1 ? "s" : "")}";
		}

		var y = time.TotalDays / avgDaysPerYear;
		return $"{y:0.#} year{(y > 1 ? "s" : "")}";
	}

	[HttpGet("stats/header")]
	[Produces("application/json")]
	public IActionResult GetHeaderData()
	{
		return Ok(new
		{
			Downloads = FormatMetric(tab.Downloads),
			RunningInstances = FormatMetric(tab.RunningInstances),
			RunningBots = FormatMetric(tab.RunningBots),
			PlaybackTime = FormatTime(tab.PlaybackTime),
		});
	}

	[HttpGet("stats/graph")]
	[Produces("application/json")]
	public IActionResult GetGraphData()
	{
		return Ok(tab.CachedDayStats);
	}

	[HttpPost("stats/update")]
	[Produces("application/json")]
	public async Task UpdateGraphData()
	{
		await tab.UpdateAggregates();
	}
}
