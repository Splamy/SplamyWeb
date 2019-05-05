using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SplamyWeb.Components;
using System;

namespace SplamyWeb
{
	public class Startup
	{
		public Startup(IConfiguration configuration, IHostingEnvironment environment)
		{
			Configuration = configuration;
			Environment = environment;
		}

		public IConfiguration Configuration { get; }
		public IHostingEnvironment Environment { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddMemoryCache();

			services
				.AddMvc()
				.AddRazorPagesOptions(options =>
				{
					options.Conventions.AuthorizeFolder("/Admin");
				});

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
			//services.AddSingleton(Environment);

			services.ConfigureApplicationCookie(options =>
			{
				// Cookie settings
				options.Cookie.HttpOnly = false;
				options.Cookie.Expiration = TimeSpan.FromDays(30);
				options.LoginPath = "~/User";
				options.LogoutPath = "~/User"; // TODO
				options.AccessDeniedPath = "~/User"; // TODO
				options.SlidingExpiration = true;
			});

			services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
			{
				EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
				ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,
			});

			services.AddHttpClient();
			services.AddHostedService<TimedTsScraper>();
			NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(Util.NLogMemory);
	}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("Error");
			}

			app.UseStatusCodePagesWithReExecute("/Error");

			app.MapWhen(
				context => context.Request.Path.ToString().EndsWith(".less"),
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
