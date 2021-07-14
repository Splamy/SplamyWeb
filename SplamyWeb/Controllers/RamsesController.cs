using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class RamsesController : ControllerBase
	{
		private readonly RamsesBackingData ramses;

		public RamsesController(RamsesBackingData ramses)
		{
			this.ramses = ramses;
		}

		[HttpGet("{key}")]
		[Produces(MediaTypeNames.Application.Json)]
		public async Task<IActionResult> Index(string key)
		{
			var entry = await ramses.Get(key);
			if (entry is null)
				return NotFound();
			return entry;
		}
	}
}
