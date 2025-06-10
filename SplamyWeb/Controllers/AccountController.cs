using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Db;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SplamyWeb.Controllers;

[Authorize]
[Route("api/[controller]")]
public class AccountController(UserManager<LoginData> userManager, SignInManager<LoginData> signInManager)
	: ControllerBase
{
	private record LoginStatus(bool LoggedIn, LoginStatusUser? User);
	private record LoginStatusUser(string? Name, int Id, UserType Rank);

	private static JsonResult GetLoggedInUser(LoginData? loginData)
	{
		if (loginData is null)
			return new JsonResult(new LoginStatus(false, null), Util.JsonWebHideNull);
		return new JsonResult(new LoginStatus(true, new LoginStatusUser(loginData.Name, loginData.Id, loginData.Rank)), Util.JsonWebHideNull);
	}

	[AllowAnonymous]
	[HttpGet("whoami")]
	public async Task<IActionResult> WhoAmI()
	{
		return GetLoggedInUser(await userManager.GetUserAsync(User));
	}

	private bool? isApi;
	private bool IsApi => isApi ??= Request.Headers.Accept.Contains(MediaTypeNames.Application.Json);

	[AllowAnonymous]
	[HttpPost("login")]
	public async Task<IActionResult> LoginAsync([FromForm] string name, [FromForm] string pass, [FromForm] bool remember)
	{
		var result = await signInManager.PasswordSignInAsync(name, pass, remember, false);
		if (result.Succeeded)
		{
			if (IsApi)
			{
				return GetLoggedInUser(await userManager.FindByNameAsync(name));
			}
			return Redirect("/");
		}
		else
		{
			var error = new[] { "PasswordMismatch" };
			return IsApi ? BadRequest(error) : Redirect($"/user/login?errors={string.Join(',', error)}");
		}
	}

	[HttpPost("update")]
	public async Task<IActionResult> UpdateAsync(
		[FromForm] int id,
		[FromForm] string? name,
		[FromForm] string? pass,
		[FromForm] string? pass_old)
	{
		var currentUser = await userManager.GetUserAsync(User);
		if (currentUser is null)
		{
			return Forbid();
		}

		LoginData? editedUser;
		if (id == currentUser.Id)
		{
			editedUser = currentUser;
		}
		else
		{
			if (!currentUser.CanEditOtherUser())
				return Forbid();
			// Admin feature
			editedUser = await userManager.FindByIdAsync(id.ToString());
			if (editedUser is null)
			{
				return NotFound("User to edit not found");
			}
		}

		if (!string.IsNullOrWhiteSpace(name))
		{
			editedUser.SetName(name);
			await userManager.UpdateAsync(editedUser);
		}

		if (!string.IsNullOrWhiteSpace(pass))
		{
			var result = await userManager.ChangePasswordAsync(editedUser, pass_old ?? "", pass);
			var errors = result.Errors.Select(e => e.Code);
			if (!result.Succeeded)
				return IsApi ? BadRequest(errors) : RedirectToPage("/user/profile", new { changepw = string.Join(",", errors) });
		}

		return IsApi ? Ok() : Redirect("/");
	}

	[HttpPost("logout")]
	public async Task<IActionResult> LogoutAsync()
	{
		await signInManager.SignOutAsync();
		return IsApi ? Ok() : Redirect("/");
	}

	public static IEnumerable<string> GetErrs(string? errs)
	{
		if (string.IsNullOrEmpty(errs)) yield break;
		foreach (var err in errs.Split(','))
		{
			switch (err)
			{
			case "PasswordMismatch": yield return "Incorrect password."; break;
			case "PasswordTooShort": yield return "Passwords must be at least 6 characters."; break;
			case "PasswordRequiresNonAlphanumeric": yield return "Passwords must have at least one non alphanumeric character."; break;
			case "PasswordRequiresLower": yield return "Passwords must have at least one lowercase ('a'-'z')."; break;
			case "PasswordRequiresUniqueChars": yield return "Passwords must use at least 3 different characters."; break;
			default: break;
			}
		}
	}

	public static string ToErrs(IdentityResult res) => string.Join("\n", res.Errors.Select(e => GetErrs(e.Code)));
}
