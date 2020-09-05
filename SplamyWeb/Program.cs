using AutoMapper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Threading.Tasks;

namespace SplamyWeb
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			IWebHost webHost = BuildWebHost(args);

			// Create a new scope
			using (var scope = webHost.Services.CreateScope())
			{
				// Get the DbContext instance
				var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();
				var context = scope.ServiceProvider.GetRequiredService<SplamyContext>();
				var db = scope.ServiceProvider.GetRequiredService<LocalDb>();

				await db.Initialize(context, mapper);
				

				//Do the migration asynchronously
				//await myDbContext.Database.MigrateAsync();
			}

			// Run the WebHost, and start accepting requests
			// There's an async overload, so we may as well use it
			await webHost.RunAsync();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost
				.CreateDefaultBuilder(args)
				.ConfigureLogging(logging =>
				{
					logging.ClearProviders();
					logging.SetMinimumLevel(LogLevel.Trace);
				})
				.UseNLog()
				.UseStartup<Startup>()
				.Build();
	}
}
