using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
			services.AddMvc();

			services.AddIdentity<LoginData, LoginData>()
				.AddDefaultTokenProviders();

			//services.AddTransient<UserManager<LoginData>>();

			services.AddTransient<IUserStore<LoginData>, LocalDb>();
			services.AddTransient<IRoleStore<LoginData>, LocalDb>();
			services.AddTransient<IUserPasswordStore<LoginData>, LocalDb>();
			services.AddTransient<IPasswordValidator<LoginData>, LocalDb>();
			services.AddTransient<IPasswordHasher<LoginData>, LocalDb>();

			services.ConfigureApplicationCookie(options =>
			{
				// Cookie settings
				options.Cookie.HttpOnly = true;
				options.Cookie.Expiration = TimeSpan.FromDays(150);
				options.LoginPath = "/Account/Login";
				options.LogoutPath = "/Account/Logout";
				options.AccessDeniedPath = "/Account/AccessDenied";
				options.SlidingExpiration = true;
			});

			services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
			{
				EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
				ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,
			});
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
			
			app.UseAuthentication();

			app.UseMvc();

			app.UseStaticFiles();
		}
	}
}
