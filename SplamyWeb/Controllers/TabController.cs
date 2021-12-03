using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Text.Json;
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
	public async Task PostPing()
	{
		var remoteIp = Request.HttpContext.Connection.RemoteIpAddress;
		if (remoteIp is null || !spam.Check(remoteIp))
			return;

		TabStatsData? obj = null;
		try
		{
			obj = await JsonSerializer.DeserializeAsync<TabStatsData?>(Request.Body, Util.JsonDefault);
		}
		catch (JsonException ex) { Log.Debug(ex, "Failed to deserialize ping"); }

		if (obj != null)
			await tab.Add(obj);
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
