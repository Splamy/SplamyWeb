using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using SplamyWeb.Components;
using SplamyWeb.Db;
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

			services.AddIdentity<LoginData, LoginData>(options =>
			{
				options.Password.RequireDigit = false;
				options.Password.RequireUppercase = false;
				options.Password.RequiredUniqueChars = 3;
				options.Password.RequireNonAlphanumeric = false;
			}).AddDefaultTokenProviders();

			services.AddAuthentication(options =>
			{
				options.AddScheme<BasicAuthenticationHandler>("BasicAuthentication", "Basic");
			});

			services.AddDbContext<SplamyContext>();
			services.AddScoped<IUserStore<LoginData>, UserStore>();
			services.AddScoped<IRoleStore<LoginData>, UserStore>();
			services.AddScoped<IUserPasswordStore<LoginData>, UserStore>();
			services.AddScoped<IPasswordValidator<LoginData>, UserStore>();
			services.AddScoped<IPasswordHasher<LoginData>, UserStore>();

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

			services.AddAutoMapper(typeof(Startup));

			services.AddSingleton<TimerService>();
			services.AddSingleton<IHostedService>(p => p.GetRequiredService<TimerService>());

			services.AddScoped<StoreService>();
			services.AddSingleton<TabBackingData>();
			services.AddSingleton<SpamBackingData>();
			services.AddSingleton<RamsesBackingData>();
#if !DEBUG
			services.AddSingleton<TeamspeakService>();
#endif

			var layout = Layout.FromString("${pad:padding=5:inner=${level:uppercase=true}} ${message} ${exception:format=ToString}");
			var config = new LoggingConfiguration();
			var consoleTarget = new ConsoleTarget { Layout = layout };
			Util.NLogMemory.Layout = layout;
			var nullTarget = new NullTarget();
			config.AddRule(LogLevel.Trace, LogLevel.Off, nullTarget, "SplamyWeb.Components.BasicAuthenticationHandler", final: true);
			config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "SplamyWeb.*");
			config.AddRule(LogLevel.Debug, LogLevel.Fatal, Util.NLogMemory, "SplamyWeb.*");

			LogManager.Configuration = config;
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IServiceProvider provider, IHostApplicationLifetime applicationLifetime)
		{
			provider.GetService<TeamspeakService>();
			provider.GetService<TabBackingData>();
			//applicationLifetime.ApplicationStopping.Register(() => { });

#if DEBUG
			var mapper = provider.GetRequiredService<IMapper>();
			mapper.ConfigurationProvider.AssertConfigurationIsValid();
#endif

#if DEBUG
			app.UseDeveloperExceptionPage();
#else
			app.UseExceptionHandler("/Error");
#endif

			app.UseStatusCodePagesWithReExecute("/Error");

			app.UseAuthentication();

			app.UseMvc();

			app.UseStaticFiles();
		}
	}
}
