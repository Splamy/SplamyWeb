using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SplamyWeb.Components;
using SplamyWeb.Db;
using SplamyWeb.Mock;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SplamyWeb.Components.Ramses;

namespace SplamyWeb;

public static class Startup
{
	public const string CorsAny = "corsAllowAny";

	public static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
	{
		if (configuration.GetValue<bool>("Mock:Web"))
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

		if (configuration.GetValue<bool>("Dev:UseCors"))
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

		services.AddMemoryCache();

		services.Configure<RamsesOptions>(configuration.GetSection("Ramses"));
		services.Configure<SplamyEnv>(paths =>
		{
			paths.DataDir = Directory.GetCurrentDirectory();
		});
		services.Configure<SplamyEnv>(configuration.GetSection("Paths"));

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

		//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
		//	.AddJwtBearer(options =>
		//	{
		//		options.TokenValidationParameters = new TokenValidationParameters
		//		{
		//			//define which claim requires to check
		//			ValidateIssuer = true,
		//			ValidateAudience = true,
		//			ValidateLifetime = true,
		//			ValidateIssuerSigningKey = true,
		//			//store the value in appsettings.json
		//			ValidIssuer = builder.Configuration["Jwt:Issuer"],
		//			ValidAudience = builder.Configuration["Jwt:Issuer"],
		//			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
		//		};
		//	});

		services.AddControllers(o =>
		{
			o.OutputFormatters.Add(new RssSerializerOutputFormatter());
		});

		services.AddResponseCompression(options =>
		{
			options.EnableForHttps = true;
			options.Providers.Add<BrotliCompressionProvider>();
			options.Providers.Add<GzipCompressionProvider>();
		});

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

		services.AddSingleton<ITimerService, TimerService>();
		services.AddHostedService(p => (TimerService)p.GetRequiredService<ITimerService>()); // Is there a better way to do this?

		services.AddScoped<StoreService>();
		services.AddSingleton<SpamBackingData>();
		services.AddSingleton<RamsesBackingData>();
		services.AddSingleton<MinigameServer>();

		services.AddSingleton<TabBackingData>();
		services.AddHostedService(x => x.GetRequiredService<TabBackingData>());
		services.AddSingleton<TeamspeakService>();
		services.AddHostedService(x => x.GetRequiredService<TeamspeakService>());
		services.AddHostedService<RamsesService>();
		services.AddHostedService(x => x.GetRequiredService<RamsesBackingData>());

		services.AddSpaStaticFiles(options => options.RootPath = "wwwroot");
	}

	// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
	public static async Task Configure(WebApplication app)
	{
		var services = app.Services;
		var env = app.Environment;

		await MigrateDatabaseAsync(services);

		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.UseExceptionHandler("/InternalError");
		}

		if (app.Configuration.GetValue<bool>("Dev:UseCors"))
		{
			app.UseCors();
		}

		app.UseResponseCompression();

		if (!env.IsDevelopment())
		{
			app.Use(async (context, next) =>
			{
				var pathStr = context.Request.Path.Value;
				if (pathStr is not (null or "/") && !context.Request.Path.StartsWithSegments("/api"))
				{
					pathStr = pathStr.TrimEnd('/');
					var lastSegment = pathStr.Substring(pathStr.LastIndexOf('/') + 1);
					if (!lastSegment.Contains('.'))
					{
						context.Request.Path = pathStr + ".html";
					}
				}

				await next.Invoke();
			});
			app.UseSpaStaticFiles();
		}
		app.UseRouting();

		app.UseAuthentication();
		app.UseAuthorization();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapControllers();
			endpoints.MapHub<MarkdownService>("/api/markdown");
			endpoints.MapHub<Minigame>("api/minigame");
		});

		app.UseWhen(
			context => !context.Request.Path.StartsWithSegments("/api"),
			then => then.UseSpa(spa =>
			{
				const int port = 3000;

				spa.Options.SourcePath = "../splamyweb_js";
				spa.Options.DevServerPort = port;
				spa.Options.PackageManagerCommand = "bun";

				if (env.IsDevelopment())
				{
					spa.UseProxyToSpaDevelopmentServer($"http://localhost:{port}");
				}
			}));
	}

	private static async Task MigrateDatabaseAsync(IServiceProvider services)
	{
		await using var scope = services.CreateAsyncScope();
		await using var context = scope.ServiceProvider.GetRequiredService<SplamyContext>();

		var logger = services.GetRequiredService<ILogger<SplamyContext>>();

		//await Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions
		//	.GetInfrastructure(context)
		//	.GetRequiredService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
		//	.MigrateAsync("20250607231335_AddGinIndex");

		var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToArray();

		if (pendingMigrations.Length > 0)
		{
			logger.LogInformation("Applying {Count} migrations.", pendingMigrations.Length);
			await context.Database.MigrateAsync();
		}

		var lastAppliedMigration = (await context.Database.GetAppliedMigrationsAsync()).Last();

		logger.LogInformation("Database on schema version: {Migration}", lastAppliedMigration);

		var env = services.GetRequiredService<IOptions<SplamyEnv>>();
		await UserStore.InitializeAccountWhenEmpty(context, logger, env.Value);
	}
}

