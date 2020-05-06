using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Text.Json;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
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
			if (!spam.Check(Request.HttpContext.Connection.RemoteIpAddress))
				return;

			TabStatsData? obj = null;
			try
			{
				obj = await JsonSerializer.DeserializeAsync<TabStatsData?>(Request.Body, Util.JsonDefault);
			}
			catch (JsonException ex) { Log.Debug(ex, "Failed to deserialize ping"); }

			if (obj != null)
				tab.Add(obj);
		}

		[HttpGet("stats/graph")]
		[Produces("application/json")]
		public IActionResult GetGraphData()
		{
			return Ok(tab.CachedDayStats);
		}
	}
}
