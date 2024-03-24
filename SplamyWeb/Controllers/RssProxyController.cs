using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace SplamyWeb.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RssProxyController(
	ILogger<RssProxyController> logger,
	IMemoryCache memoryCache,
	HttpClient httpClient
	) : ControllerBase
{
	public record KeyValue(string Key, string? Value);

	[HttpGet("cassandra")]
	public async Task<IActionResult> Get(CancellationToken cancellationToken)
	{
		var req = await httpClient.GetAsync("https://tapastic.com/rss/series/35182", cancellationToken);
		var content = await req.Content.ReadAsStringAsync(cancellationToken);

		var xml = new XmlDocument();
		xml.LoadXml(content);

		var items = xml.SelectNodes("//rss/channel/item");

		foreach (var node in items.Cast<XmlNode>())
		{
			var link = node.SelectSingleNode("link")!.InnerText;
			var cacheKey = new CassandraLink(link);

			if (!memoryCache.TryGetValue(cacheKey, out string? cachedHtml))
			{
				logger.LogInformation("Fetching RSS Cassandra {Link}", link);
				var fullHtml = await httpClient.GetStringAsync(link, cancellationToken);

				var doc = new HtmlDocument();
				doc.LoadHtml(fullHtml);
				var article = doc.DocumentNode.SelectSingleNode("//article");

				cachedHtml = article.InnerHtml;

				memoryCache.Set(cacheKey, cachedHtml, TimeSpan.FromDays(1));
			}

			var rssContent = node["content:encoded"]!;
			rssContent.InnerText = cachedHtml!;
		}

		return Content(xml.OuterXml, "application/rss+xml", Encoding.UTF8);
	}

	private record CassandraLink(string Link);
}
