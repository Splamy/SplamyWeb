using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace SplamyWeb.Controllers
{
	[AllowAnonymous]
	public class Fallback : ControllerBase
	{
		private readonly IWebHostEnvironment appEnvironment;

		public Fallback(IWebHostEnvironment appEnvironment)
		{
			this.appEnvironment = appEnvironment;
		}

		public IActionResult Index()
		{
			var reqPath = Request.Path.Value;
			if (reqPath is null || reqPath.Contains('.'))
				return NotFound();

			const string IndexEndStrip = "index.html";
			if (reqPath.EndsWith(IndexEndStrip, StringComparison.OrdinalIgnoreCase))
				reqPath = reqPath[..^IndexEndStrip.Length];

			var fullReqPath = Path.Join(appEnvironment.WebRootPath, reqPath);
			try
			{
				var target = Directory.GetFiles(fullReqPath, "index.html", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }).FirstOrDefault();
				if (target is not null)
				{
					var targetPath = target.AsSpan();
					targetPath = targetPath[..fullReqPath.Length];
					if (targetPath.EndsWith(IndexEndStrip, StringComparison.OrdinalIgnoreCase))
						targetPath = targetPath[..^IndexEndStrip.Length];
					targetPath = targetPath.TrimEnd(new char[] { '\\', '/' });
					return RedirectPermanent(targetPath.ToString());
				}
			}
			catch { }

			return Redirect("/error?req=" + Uri.EscapeDataString(Request.Path.Value ?? ""));
		}
	}
}
