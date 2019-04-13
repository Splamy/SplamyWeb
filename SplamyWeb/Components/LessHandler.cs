using dotless.Core;
using dotless.Core.configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Threading.Tasks;

namespace SplamyWeb.Components
{
	public class LessHandler
	{
		private readonly IMemoryCache memoryCache;
		private readonly IHostingEnvironment env;
		private static readonly DotlessConfiguration conf = new DotlessConfiguration()
		{
			MinifyOutput = true,
			Debug = false,
		};

		public LessHandler(RequestDelegate _, IHostingEnvironment env, IMemoryCache memoryCache)
		{
			this.env = env;
			this.memoryCache = memoryCache;
		}

		public async Task Invoke(HttpContext context)
		{
			string response = GenerateResponse(context);

			context.Response.ContentType = GetContentType();
			await context.Response.WriteAsync(response);
		}

		private string GenerateResponse(HttpContext context)
		{
			var file = context.Request.Path.ToString();
			try
			{
				return memoryCache.GetOrCreate(file, ce =>
				{
					var data = Less.Parse(File.ReadAllText(env.WebRootFileProvider.GetFileInfo((string)ce.Key).PhysicalPath), conf);
					ce.AddExpirationToken(env.WebRootFileProvider.Watch((string)ce.Key));
					return data;
				});
			}
			catch
			{
				context.Response.StatusCode = 404;
				return string.Empty;
			}
		}

		private string GetContentType()
		{
			return "text/css";
		}
	}

	public static class LessHandlerExtensions
	{
		public static IApplicationBuilder UseLessHandler(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<LessHandler>();
		}
	}
}
