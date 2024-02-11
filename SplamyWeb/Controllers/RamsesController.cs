using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
using System.IO.Compression;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Formats.Tar;
using Microsoft.AspNetCore.Authorization;
using static SplamyWeb.Util;
using System.Threading;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RamsesController(RamsesBackingData ramses) : ControllerBase
{
	[HttpGet("m/{key}")]
	[Produces(MediaTypeNames.Application.Json)]
	public async Task<IActionResult> GetMapRate(string key)
	{
		var entry = await ramses.Get(key);
		if (entry is null)
			return NotFound();
		return entry;
	}

	[HttpGet("raw/{count}")]
	[Authorize(AuthenticationSchemes = AuthScheme)]
	public async Task GetMapsZip(int count, CancellationToken cancellationToken)
	{
		//count = Math.Clamp(count, 1, 100);

		Response.Headers.ContentDisposition = "attachment; filename=maps.tar.gz";

		var stream = Response.Body;

		await using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
		await using var tar = new TarWriter(gzip, leaveOpen: true);

		await foreach (var (id, provider) in ramses.GetMaps(cancellationToken))
		{
			if (count-- <= 0)
				break;

			var mapIdStr = id.ToString("X2");

			foreach (var file in provider.Files)
			{
				await using var fileStream = provider.Get(file);

				if (fileStream is null)
					continue;

				var entry = new V7TarEntry(TarEntryType.V7RegularFile, mapIdStr + "/" + file)
				{
					Mode = UnixFileMode.UserRead | UnixFileMode.GroupRead | UnixFileMode.OtherRead | UnixFileMode.UserWrite | UnixFileMode.GroupWrite | UnixFileMode.OtherWrite,
					DataStream = fileStream,
				};

				await tar.WriteEntryAsync(entry, cancellationToken);
			}
		}
	}
}
