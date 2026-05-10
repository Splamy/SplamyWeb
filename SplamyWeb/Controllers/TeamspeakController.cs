using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamspeakController(TeamspeakService tsService) : ControllerBase
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	[HttpPost("version/{build}/{platform}")]
	[Produces("application/json")]
	public async Task<IActionResult> AddNewVersionSign(string build, string platform, [FromQuery] string sign)
	{
		Log.Debug("Got version request for {0},{1}", build, platform);

		//var contentType = this.Request.ContentType;
		var vsign = new VersionSign(build, platform, sign);
		var checkResult = TeamspeakService.CheckVersion(vsign);
		if (checkResult != null)
		{
			if (checkResult.FixedVersion != null)
				vsign = checkResult.FixedVersion;
			else
				return UnprocessableEntity(checkResult);
		}

		return await tsService.TryAddNewVersionSign(vsign);
	}
}
