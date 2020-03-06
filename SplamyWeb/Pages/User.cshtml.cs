using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Linq;
using System.Collections.Generic;

namespace SplamyWeb.Pages
{
	public class UserModel : PageModel
	{
		[BindProperty]
		public LoginData LoginData { get; set; }

		private readonly UserManager<LoginData> userManager;

		public UserModel(UserManager<LoginData> userManager)
		{
			this.userManager = userManager;
			LoginData = null!;
		}

		public async void OnGet()
		{
			LoginData = await userManager.GetUserAsync(User);
		}

		public IEnumerable<string> ErrorLogin()
		{
			var errs = HttpContext.Request.Query["login"].FirstOrDefault();
			return GetErrs(errs);
		}

		public IEnumerable<string> ErrorChangePw()
		{
			var errs = HttpContext.Request.Query["changepw"].FirstOrDefault();
			return GetErrs(errs);
		}

		public IEnumerable<string> GetErrs(string? errs)
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
	}
}
