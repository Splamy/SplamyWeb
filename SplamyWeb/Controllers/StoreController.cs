using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Linq;
using System.Threading.Tasks;
using static SplamyWeb.Util;

namespace SplamyWeb.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthScheme)]
[Route("api/[controller]")]
public class StoreController(StoreService store) : ControllerBase
{
	public record KeyValue(string Key, string? Value);

	[HttpGet("all")]
	public async Task<IEnumerable<KeyValue>> GetAll() => (await store.GetAll()).Select(kvp => new KeyValue(kvp.Id, kvp.Value));

	[HttpGet("value/{key}")]
	public async Task<string?> Get(string key) => await store.Get(key);

	[HttpDelete("value/{key}")]
	public async Task Delete(string key) => await store.Delete(key);

	[HttpPut("value/{key}")]
	public async Task Put(string key, [FromQuery] string value) => await store.Set(key, value);
}
