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
				obj = await JsonSerializer.DeserializeAsync<TabStatsData?>(Request.Body);
			}
			catch (JsonException) { }

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
