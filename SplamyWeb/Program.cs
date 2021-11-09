using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using SplamyWeb.Db;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb;

public static class Program
{
	public static async Task Main(string[] args)
	{
		ServerLog.ConfigueNLog();

		IWebHost webHost = BuildWebHost(args);

		// Create a new scope
		using (var scope = webHost.Services.CreateScope())
		{
			using var context = scope.ServiceProvider.GetRequiredService<SplamyContext>();

			var logger = NLog.LogManager.GetLogger("SplamyWeb.Startup");

			var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToArray();

			//await context.GetInfrastructure().GetRequiredService<IMigrator>().MigrateAsync("20210714165443_InitialCreate");
			if (pendingMigrations.Length > 0)
			{
				logger.Info($"Applying {pendingMigrations.Length} migrations.");
				await context.Database.MigrateAsync();
			}

			var lastAppliedMigration = (await context.Database.GetAppliedMigrationsAsync()).Last();

			logger.Info($"Database on schema version: {lastAppliedMigration}");
		}

		// Run the WebHost, and start accepting requests
		// There's an async overload, so we may as well use it
		await webHost.RunAsync();
	}

	public static IWebHost BuildWebHost(string[] args) =>
		WebHost
			.CreateDefaultBuilder(args)
			.ChangePortSomewhereElse()
			.ConfigureLogging(logging =>
			{
				logging.ClearProviders();
				logging.SetMinimumLevel(LogLevel.Trace);
			})
			.UseNLog()
			.UseStartup<Startup>()
			.Build();

	private static IWebHostBuilder ChangePortSomewhereElse(this IWebHostBuilder webHostBuilder)
		=> Environment.CurrentDirectory.StartsWith("E", StringComparison.OrdinalIgnoreCase) ? webHostBuilder.UseUrls("http://*:44422") : webHostBuilder;
}
