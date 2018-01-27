using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SplamyWeb.Controllers
{
	[Route("[controller]")]
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
		public async Task<IActionResult> Login([FromForm] string name, [FromForm] string pass, [FromForm] bool remember)
		{
			var result = await signInManager.PasswordSignInAsync(name, pass, remember, false);
			if (result.Succeeded)
				return RedirectToPage("/Index");
			else
				return RedirectToPage("/User", new { error = 1 });
		}

		[HttpPost("Update")]
		[Authorize]
		public async Task<IActionResult> Update([FromForm] LoginData upuser)
		{
			var user = await userManager.GetUserAsync(User);
			await userManager.UpdateAsync(upuser);
			return RedirectToPage("/User");
		}

		[HttpPost("Logout")]
		[Authorize]
		public async Task<IActionResult> Logout()
		{
			await signInManager.SignOutAsync();
			return RedirectToPage("/Index");
		}
	}
}
