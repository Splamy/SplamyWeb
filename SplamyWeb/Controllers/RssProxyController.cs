using System.Collections.Immutable;
using System.IO;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    private static readonly string CachePath = Path.Combine(Util.DataPath, "rss_cache");

    [HttpGet("cassandra")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var req = await httpClient.GetAsync("https://tapas.io/rss/series/35182", cancellationToken);
        var content = await req.Content.ReadAsStringAsync(cancellationToken);

        var xml = new XmlDocument();
        xml.LoadXml(content);

        var items = xml.SelectNodes("//rss/channel/item");

        foreach (var node in items.Cast<XmlNode>())
        {
            var link = node.SelectSingleNode("link")!.InnerText;
            var cacheKey = new CachedLink(link);

            if (!memoryCache.TryGetValue(cacheKey, out string? cachedHtml))
            {
                logger.LogInformation("Fetching RSS Cassandra {Link}", link);
                var fullHtml = await httpClient.GetStringAsync(link, cancellationToken);

                var doc = new HtmlDocument();
                doc.LoadHtml(fullHtml);
                var article = doc.DocumentNode.SelectSingleNode("//article");

                foreach (var img in article.SelectNodes("img"))
                {
                    if (img.Attributes.Contains("data-src"))
                    {
                        var src = img.Attributes["data-src"].Value;

                        img.Attributes.Remove("src");
                        img.Attributes.Remove("data-src");

                        img.Attributes.Add("src", src);
                    }
                }

                cachedHtml = article.InnerHtml;

                memoryCache.Set(cacheKey, cachedHtml, TimeSpan.FromDays(1));
            }

            var rssContent = node["content:encoded"]!;
            rssContent.InnerText = cachedHtml!;
        }

        return Content(xml.OuterXml, "application/rss+xml", Encoding.UTF8);
    }

    [HttpGet("satw")]
    public async Task<IActionResult> GetSatw(CancellationToken cancellationToken)
    {
        var req = await httpClient.GetAsync("http://feeds.feedburner.com/satwcomic", cancellationToken);
        var content = await req.Content.ReadAsStringAsync(cancellationToken);

        var xml = new XmlDocument();
        xml.LoadXml(content);

        var items = xml.SelectNodes("//rss/channel/item");

        foreach (var node in items.Cast<XmlNode>())
        {
            var link = node.SelectSingleNode("link")!.InnerText;
            var cacheKey = new CachedLink(link);

            if (!memoryCache.TryGetValue(cacheKey, out string? cachedHtml))
            {
                logger.LogInformation("Fetching RSS SATW {Link}", link);
                var fullHtml = await httpClient.GetStringAsync(link, cancellationToken);

                var doc = new HtmlDocument();
                doc.LoadHtml(fullHtml);
                var article = doc.DocumentNode.SelectSingleNode("//div[@class='card shadow']");

                cachedHtml = article.InnerHtml;

                memoryCache.Set(cacheKey, cachedHtml, TimeSpan.FromDays(1));
            }

            // Create new node and wirte inner html
            var rssContent = node["description"];
            if (rssContent is null)
            {
                rssContent = xml.CreateElement("description");
                node.AppendChild(rssContent);
            }

            rssContent.InnerText = cachedHtml!;
        }

        return Content(xml.OuterXml, "application/rss+xml", Encoding.UTF8);
    }


    [HttpGet("rhythm_quest")]
    public async Task<IActionResult> GetRhythmQuest(CancellationToken cancellationToken)
    {
        var rssTemplate = """
            <?xml version="1.0" encoding="UTF-8" ?>
            <rss xmlns:content="http://purl.org/rss/1.0/modules/content/" version="2.0">
                <channel>
                    <title>Rhythm Quest</title>
                    <description>Rhythm Quest Devlog</description>
                    <link>https://rhythmquestgame.com/devlog/devlog.html</link>
                    <copyright>2020 Example.com All rights reserved</copyright>
                    <lastBuildDate>Mon, 6 Sep 2010 00:01:00 +0000</lastBuildDate>
                    <pubDate>Sun, 6 Sep 2009 16:20:00 +0000</pubDate>
                    <ttl>86400</ttl>
                </channel>
            </rss>
            """;

        var xml = new XmlDocument();
        xml.LoadXml(rssTemplate);
        var channelNode = xml.SelectSingleNode("//rss/channel")!;

        var baseUri = new Uri("https://rhythmquestgame.com/devlog/");

        var req = await httpClient.GetAsync(new Uri(baseUri, "devlog.html"), cancellationToken);
        var content = await req.Content.ReadAsStringAsync(cancellationToken);
        var html = new HtmlDocument();
        html.LoadHtml(content);

        // select: .wrapper p a
        var lastEntries = html.DocumentNode.SelectNodes("//div[contains(@class, 'wrapper')]//p//a")
            .TakeLast(10)
            .ToArray();

        foreach (var entry in lastEntries)
        {
            var itemLink = new Uri(baseUri, entry.Attributes["href"].Value);
            var linkHash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(itemLink.ToString())));
            var cacheFile = Path.Combine(CachePath, $"{linkHash}.html");

            string entryHtmlString;

            if (!System.IO.File.Exists(cacheFile))
            {
                logger.LogInformation("Fetching Rhythm Quest devlog entry {Link}", itemLink);
                await Task.Delay(500, cancellationToken); // be polite
                var itemReq = await httpClient.GetAsync(itemLink, cancellationToken);
                entryHtmlString = await itemReq.Content.ReadAsStringAsync(cancellationToken);
                var directory = Path.GetDirectoryName(cacheFile)!;
				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}
                await System.IO.File.WriteAllTextAsync(cacheFile, entryHtmlString, cancellationToken);
            }
            else
            {
                logger.LogInformation("Using cached Rhythm Quest devlog entry {Link}", itemLink);
                entryHtmlString = await System.IO.File.ReadAllTextAsync(cacheFile, cancellationToken);
            }

            var entryHtml = new HtmlDocument();
            entryHtml.LoadHtml(entryHtmlString);
            var entryDoc = entryHtml.DocumentNode;
            entryDoc.SelectSingleNode("//header")?.Remove();

            var item = xml.CreateElement("item");

            var title = xml.CreateElement("title");
            title.InnerText = entryDoc.SelectSingleNode("//h1")?.InnerText.Trim() ?? "No title";
            item.AppendChild(title);

            var entryContent = entryDoc.SelectSingleNode("//div[contains(@class, 'wrapper')]");
            var itemDescriptionContent = entryContent?.InnerHtml.Trim() ?? "No content";
            var description = xml.CreateElement("description");
            description.InnerText = itemDescriptionContent;
            item.AppendChild(description);

            var link = xml.CreateElement("link");
            link.InnerText = itemLink.ToString();
            item.AppendChild(link);

            var guid = xml.CreateElement("guid");
            guid.SetAttribute("isPermaLink", "false");
            guid.InnerText = Guid.NewGuid().ToString();
            item.AppendChild(guid);

            // "Published: December 1, 2025"
            var pubText = entryDoc.SelectSingleNode("//p[contains(text(), 'Published:')]")?.InnerText.Trim();
            Regex pubRegex = new(@"Published:\s*(?<MonthName>\w+)\s+(?<Day>\d+)\s*,\s*(?<Year>\d+)", RegexOptions.IgnoreCase);
            var pubDateParsed = DateTime.UtcNow;
            if (pubText != null && pubRegex.Match(pubText) is { Success: true } match)
            {
                pubDateParsed = new DateTime(
                    int.Parse(match.Groups["Year"].Value),
                    Months.IndexOf(match.Groups["MonthName"].Value) + 1,
                    int.Parse(match.Groups["Day"].Value));
            }

            var pubDate = xml.CreateElement("pubDate");
            pubDate.InnerText = pubDateParsed.ToString("R");
            item.AppendChild(pubDate);

            channelNode.AppendChild(item);
        }

        return Content(xml.OuterXml, "application/rss+xml", Encoding.UTF8);
    }

    private static readonly ImmutableArray<string> Months =
    [
        "January", "February", "March", "April", "May", "June",
		"July", "August", "September", "October", "November", "December"
    ];

    private record CachedLink(string Link);
}
