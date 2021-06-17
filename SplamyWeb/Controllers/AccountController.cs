using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SplamyWeb.Db;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SplamyWeb.Controllers
{
	[Authorize]
	[Route("[controller]")]
	public class AccountController : ControllerBase
	{
		private readonly UserManager<LoginData> userManager;
		private readonly SignInManager<LoginData> signInManager;

		public AccountController(UserManager<LoginData> userManager, SignInManager<LoginData> signInManager)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
		}

		[AllowAnonymous]
		[HttpPost("Login")]
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
			var currentUser = await userManager.GetUserAsync(User);
			LoginData editedUser;
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
					return NotFound("User to edit not found");
			}

			if (!string.IsNullOrWhiteSpace(name))
			{
				editedUser.SetName(name);
				await userManager.UpdateAsync(editedUser);
			}

			if (!string.IsNullOrWhiteSpace(pass))
			{
				var result = await userManager.ChangePasswordAsync(editedUser, pass_old ?? "", pass);
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

		private static string ToErrs(IdentityResult res) => string.Join(",", res.Errors.Select(e => e.Code));
	}
}
