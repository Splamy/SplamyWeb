using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Db;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SplamyWeb.Controllers
{
	[Route("[controller]")]
	[Authorize]
	public class AccountController : Controller
	{
		private readonly UserManager<LoginData> userManager;
		private readonly SignInManager<LoginData> signInManager;

		public AccountController(UserManager<LoginData> userManager, SignInManager<LoginData> signInManager)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
		}

		[HttpPost("Login")]
		[AllowAnonymous]
		public async Task<IActionResult> LoginAsync([FromForm] string name, [FromForm] string pass, [FromForm] bool remember)
		{
			var result = await signInManager.PasswordSignInAsync(name, pass, remember, false);
			if (result.Succeeded)
				return RedirectToPage("/Index");
			else
				return RedirectToPage("/User", new { login = "PasswordMismatch" });
		}

		[HttpPost("Update")]
		public async Task<IActionResult> UpdateAsync(
			[FromForm] int id,
			[FromForm] string? name,
			[FromForm] string? pass,
			[FromForm] string? pass_old)
		{
			var user = await userManager.GetUserAsync(User);
			if (id != user.Id && !user.CanEditOtherUser())
			{
				return Forbid();
			}
			else
			{
				// Admin feature
			}

			if (!string.IsNullOrWhiteSpace(pass))
			{
				var result = await userManager.ChangePasswordAsync(user, pass_old ?? "", pass);
				if (!result.Succeeded)
					return RedirectToPage("/User", new { changepw = ToErrs(result) });
			}

			return RedirectToPage("/User", new { });
		}

		[HttpPost("Logout")]
		public async Task<IActionResult> LogoutAsync()
		{
			await signInManager.SignOutAsync();
			return RedirectToPage("/Index");
		}

		private string ToErrs(IdentityResult res) => string.Join(",", res.Errors.Select(e => e.Code));
	}
}
