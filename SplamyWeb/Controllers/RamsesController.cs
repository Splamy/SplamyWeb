using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers
{
	[Route("api/[controller]")]
	public class RamsesController : Controller
	{
		private readonly RamsesBackingData ramses;

		public RamsesController(RamsesBackingData ramses)
		{
			this.ramses = ramses;
		}

		[HttpGet("{key}")]
		public async Task<IActionResult> Index(string key)
		{
			var entry = await ramses.Get(key);
			if (entry is null)
				return Problem("Error Processing");
			return Ok(entry);
		}
	}
}
