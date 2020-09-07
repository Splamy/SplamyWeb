using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Threading.Tasks;

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
		public async Task<string?> Get(string key) => await store.Get(key);

		[HttpDelete("value/{key}")]
		public async Task Delete(string key) => await store.Delete(key);

		[HttpPut("value/{key}")]
		public async Task Put(string key, [FromQuery] string? value) => await store.Set(key, value);
	}
}
