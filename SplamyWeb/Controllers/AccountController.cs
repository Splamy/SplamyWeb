using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Components;
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
			var result = await signInManager.PasswordSignInAsync(name, pass, remember, false).ConfigureAwait(false);
			if (result.Succeeded)
				return RedirectToPage("/Index");
			else
				return RedirectToPage("/User", new { error = 1 });
		}

		[HttpPost("Update")]
		public async Task<IActionResult> UpdateAsync([FromForm] LoginData upuser)
		{
			// Uuuh, splamy, waddaya do?
			var user = await userManager.GetUserAsync(User).ConfigureAwait(false);
			await userManager.UpdateAsync(upuser).ConfigureAwait(false);
			return RedirectToPage("/User");
		}

		[HttpPost("Logout")]
		public async Task<IActionResult> LogoutAsync()
		{
			await signInManager.SignOutAsync().ConfigureAwait(false);
			return RedirectToPage("/Index");
		}
	}
}
