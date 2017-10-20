using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SplamyWeb.Controllers
{
	[Route("[controller]")]
	public class AccountController : Controller
	{
		private readonly UserManager<LoginData> _userManager;
		private readonly SignInManager<LoginData> _signInManager;

		public AccountController(UserManager<LoginData> userManager, SignInManager<LoginData> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		[HttpPost("Login")]
		public async Task<IActionResult> Login([FromForm] string name, [FromForm] string pass)
		{
			var result = await _signInManager.PasswordSignInAsync(name, pass, true, false);
			if (result.Succeeded)
				return RedirectToPage("/Index");
			else
				return RedirectToPage("/User");
		}

		[HttpPost("Logout")]
		[Authorize]
		public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
			return RedirectToPage("/Index");
		}
	}
}
