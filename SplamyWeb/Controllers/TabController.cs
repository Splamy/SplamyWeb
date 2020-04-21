using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SplamyWeb.Components;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static SplamyWeb.Util;

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

			TabStatsData? obj;

			using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
			using (var jsonReader = new JsonTextReader(reader))
			{
				var json = await JObject.LoadAsync(jsonReader);
				obj = json.ToObject<TabStatsData>();
			}

			if (obj != null)
				tab.Add(obj);
		}

		[HttpGet("check")]
		[Produces("application/json")]
		//[Authorize(AuthenticationSchemes = AuthScheme)]
		public IActionResult GetCheck()
		{
			var entry = tab.Get();
			return Ok(entry);
		}

		[HttpGet("stats/graph")]
		[Produces("application/json")]
		public IActionResult GetGraphData()
		{
			return Ok(tab.CachedDayStats);
		}
	}
}
