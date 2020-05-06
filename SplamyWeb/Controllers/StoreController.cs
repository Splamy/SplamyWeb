using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	[Authorize]
	public class StoreController : ControllerBase
	{
		private readonly StoreService store;

		public StoreController(StoreService store)
		{
			this.store = store;
		}

		[HttpGet("value/{key}")]
		public string? Get(string key) => store.Get(key);

		[HttpDelete("value/{key}")]
		public void Delete(string key) => store.Delete(key);

		[HttpPut("value/{key}")]
		public void Put(string key, [FromQuery]string? value) => store.Set(key, value);
	}
}
