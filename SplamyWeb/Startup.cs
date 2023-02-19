using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SplamyWeb.Components;
using SplamyWeb.Db;
using SplamyWeb.Mock;
using System.Net.Http;
using System.Text.Json;

namespace SplamyWeb;

public class Startup
{
	public const string CorsAny = "corsAllowAny";

	public Startup(IConfiguration configuration)
	{
		Configuration = configuration;
	}

	public IConfiguration Configuration { get; }

	// This method gets called by the runtime. Use this method to add services to the container.
	public void ConfigureServices(IServiceCollection services)
	{
		if (Configuration.GetValue<bool>("Mock:Web"))
		{
			services.AddSingleton<IHttpClientFactory, MockedHttpClientFactory>();
		}
		else
		{
			services.AddHttpClient(Options.DefaultName, client =>
			{
				client.DefaultRequestHeaders.UserAgent.Clear();
				client.DefaultRequestHeaders.UserAgent.Add(new("SplamyWeb", "1.0.0"));
			});
		}

		if (Configuration.GetValue<bool>("Dev:UseCors"))
		{
			services.AddCors(options =>
			{
				options.AddPolicy(CorsAny,
					builder =>
					{
						builder.AllowAnyOrigin();
					});
				options.AddDefaultPolicy(
					builder =>
					{
						builder.WithOrigins("http://localhost:3000", "http://localhost:44422");
						builder.AllowCredentials();
						builder.AllowAnyHeader();
						builder.AllowAnyMethod();
					});
			});
		}

		services.AddSignalR().AddJsonProtocol(options =>
		{
			options.PayloadSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
			{
				Converters = { new Vector2Converter() }
			};
		});

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

		services.AddControllers(o =>
		{
			o.OutputFormatters.Add(new RssSerializerOutputFormatter());
		});

		services.AddResponseCompression();

		services.AddSingleton(p =>
		{
			var conf = p.GetRequiredService<IConfiguration>();
			var env = p.GetRequiredService<IWebHostEnvironment>();

			return new DbContextConfig(
				conf.GetConnectionString("DefaultConnection") ?? throw new Exception("Missing db connection string"),
				env.IsDevelopment()
			);
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
			options.LoginPath = "/user";
			options.LogoutPath = "/account/logout";
			options.SlidingExpiration = true;
		});

		services.AddDataProtection().UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration()
		{
			EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
			ValidationAlgorithm = ValidationAlgorithm.HMACSHA256,
		});

		services.AddAutoMapper(typeof(Startup));

		services.AddSingleton<TimerService>();
		services.AddHostedService(p => p.GetRequiredService<TimerService>());

		services.AddScoped<StoreService>();
		services.AddSingleton<TabBackingData>();
		services.AddSingleton<SpamBackingData>();
		services.AddSingleton<RamsesBackingData>();
		services.AddSingleton<TeamspeakService>();
		services.AddSingleton<LogNotifierService>();
		services.AddSingleton<MinigameServer>();

		services.AddHostedService<RamsesService>();
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public void Configure(IApplicationBuilder app, IServiceProvider provider, IWebHostEnvironment env)
	{
		provider.GetService<LogNotifierService>();
		provider.GetService<TeamspeakService>();
		provider.GetService<TabBackingData>();

		if (env.IsDevelopment())
		{
			var mapper = provider.GetRequiredService<AutoMapper.IMapper>();
			mapper.ConfigurationProvider.AssertConfigurationIsValid();

			app.UseDeveloperExceptionPage();

		}
		else
		{
			app.UseExceptionHandler("/InternalError");
		}

		if (Configuration.GetValue<bool>("Dev:UseCors"))
		{
			app.UseCors();
		}

		app.UseResponseCompression();

		app.UseFileServer(new FileServerOptions
		{
			RedirectToAppendTrailingSlash = false,
		});

		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
			endpoints.MapHub<LogNotifier>("/livelog");
			endpoints.MapHub<MarkdownService>("/markdown");
			endpoints.MapHub<Minigame>("/minigame");
			endpoints.MapFallbackToController("Index", "Fallback");
		});
	}
}
