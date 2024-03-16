using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SplamyWeb.Mock;

public class MockedHttpClientFactory : IHttpClientFactory
{
	public HttpClient CreateClient(string name)
	{
		return new HttpClient(new MockedHttpMessageHandler());
	}
}

class MockHttpClient : HttpClient
{
	public MockHttpClient() : base(new MockedHttpMessageHandler())
	{

	}
}

internal class MockedHttpMessageHandler : HttpClientHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request.RequestUri!.AbsoluteUri.MatchPrefix("https://beatsaver.com/api/download/key/", StringComparison.OrdinalIgnoreCase, out var rest))
		{
			var dir = Directory.EnumerateDirectories(@"F:\SteamLibrary\steamapps\common\Beat Saber\Beat Saber_Data\CustomLevels").FirstOrDefault(dir => dir.Contains(rest, StringComparison.OrdinalIgnoreCase));
			if (dir == default)
			{
				return Task.FromResult(new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound, Content = new StringContent("") });
			}
			else
			{
				var mem = new MemoryStream();
				using (var zip = new ZipArchive(mem, ZipArchiveMode.Create, true))
				{
					foreach (var file in Directory.EnumerateFiles(dir))
					{
						zip.CreateEntryFromFile(file, Path.GetFileName(file));
					}
				}
				mem.Position = 0;
				return Task.FromResult(new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(mem) });
			}
		}
		else
		{
			return base.SendAsync(request, cancellationToken);
		}
	}
}
