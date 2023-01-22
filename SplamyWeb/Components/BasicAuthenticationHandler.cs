using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SplamyWeb.Db;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SplamyWeb.Components;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
	private readonly UserManager<LoginData> userManager;
	private readonly SignInManager<LoginData> signInManager;

	public BasicAuthenticationHandler(
		IOptionsMonitor<AuthenticationSchemeOptions> options,
		ILoggerFactory logger,
		UrlEncoder encoder,
		ISystemClock clock,
		UserManager<LoginData> userManager,
		SignInManager<LoginData> signInManager) : base(options, logger, encoder, clock)
	{
		this.userManager = userManager;
		this.signInManager = signInManager;
	}

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.ContainsKey("Authorization"))
			return AuthenticateResult.Fail("Missing Authorization header");

		LoginData? user = null;
		try
		{
			var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
			if (authHeader.Parameter is null)
				return AuthenticateResult.Fail("Missing or empty Authorization header");
			var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
			var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
			var username = credentials[0];
			var password = credentials[1];
			var getUser = await userManager.FindByNameAsync(username);
			if (getUser is null)
			{
				return AuthenticateResult.Fail("Invalid Username or Password");
			}
			var signInCheck = await signInManager.CheckPasswordSignInAsync(getUser, password, false);
			if (signInCheck.Succeeded)
			{
				user = getUser;
			}
		}
		catch
		{
			return AuthenticateResult.Fail("Invalid Authorization header");
		}

		if (user is null)
			return AuthenticateResult.Fail("Invalid Username or Password");

		var claims = new[] {
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
			new Claim(ClaimTypes.Name, user.Name),
		};
		var identity = new ClaimsIdentity(claims, Scheme.Name);
		var principal = new ClaimsPrincipal(identity);
		var ticket = new AuthenticationTicket(principal, Scheme.Name);

		return AuthenticateResult.Success(ticket);
	}
}
