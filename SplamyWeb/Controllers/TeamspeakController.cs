using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamspeakController : ControllerBase
{
	private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	private readonly TeamspeakService tsService;

	public TeamspeakController(TeamspeakService tsService)
	{
		this.tsService = tsService;
	}

	[HttpPost("github")]
	[Consumes("application/json")] // , "text/csv"
	public async Task<IActionResult> GithubPullRequest([FromBody] Json_Github? json) // [FromQuery] string token, 
	{
		//var user = LocalDb.GetUserByToken(token);
		//if (user == null)
		//	return BadRequest("Not authorized");

		if (json?.pull_request != null)
		{
			if (json.pull_request.state == "closed")
				return Ok();

			var (safe, affectsVersion) = await TeamspeakService.CheckSafeToAccept(json.pull_request.diff_url);

			Log.Debug("PR #{0} is safe:{1} version:{2}", json.pull_request.number, safe, affectsVersion);
		}

		Log.Debug("Action: {@data}", json);

		return Ok();
	}

	//https://api.github.com/repos/Splamy/TravisExperiments/contents/ts3notify.sh

	// , "text/csv"
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
