using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using SplamyWeb.Components;
using System;

namespace SplamyWeb
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMemoryCache();

			services
			   .AddMvc(options =>
				{
					options.EnableEndpointRouting = false;
				})
				.AddRazorPagesOptions(options =>
				{
					options.Conventions.AuthorizeFolder("/Admin");
				})
#if DEBUG
				.AddRazorRuntimeCompilation()
#endif
			;

			services.AddIdentity<LoginData, LoginData>()
				.AddDefaultTokenProviders();

			services.AddAuthentication(options =>
			{
				options.AddScheme<BasicAuthenticationHandler>("BasicAuthentication", "Basic");
			});

			var db = new LocalDb();
			services.AddSingleton(db);
			services.AddSingleton<IUserStore<LoginData>>(db);
			services.AddSingleton<IRoleStore<LoginData>>(db);
			services.AddSingleton<IUserPasswordStore<LoginData>>(db);
			services.AddSingleton<IPasswordValidator<LoginData>>(db);
			services.AddSingleton<IPasswordHasher<LoginData>>(db);

			services.ConfigureApplicationCookie(options =>
			{
				// Cookie settings
				options.Cookie.HttpOnly = false;
				options.ExpireTimeSpan = TimeSpan.FromDays(30);
				options.LoginPath = "/User";
				options.LogoutPath = "/User"; // TODO
				options.AccessDeniedPath = "/User"; // TODO
				options.SlidingExpiration = true;
			});

			services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
			{
				EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
				ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,
			});

			services.AddHttpClient();
			services.AddHostedService<TimedTsScraper>();

			var config = new LoggingConfiguration();
			var consoleTarget = new ConsoleTarget();
			config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget, "SplamyWeb.*");
			config.AddRule(LogLevel.Info, LogLevel.Fatal, Util.NLogMemory, "SplamyWeb.*");
			LogManager.Configuration = config;
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app)
		{
#if DEBUG
			app.UseDeveloperExceptionPage();
#else
			app.UseExceptionHandler("/Error");
#endif

			app.UseStatusCodePagesWithReExecute("/Error");

			app.MapWhen(
				context => context.Request.Path.ToString().EndsWith(".less", StringComparison.Ordinal),
				appBranch =>
				{
					// ... optionally add more middleware to this branch
					appBranch.UseLessHandler();
				});

			app.UseAuthentication();

			app.UseMvc();

			app.UseStaticFiles();
		}
	}
}
