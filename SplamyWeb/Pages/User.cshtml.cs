
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;

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
		}

		public async void OnGet()
		{
			LoginData = await userManager.GetUserAsync(User);
		}

		public string Error()
		{
			switch (HttpContext.Request.Query["error"])
			{
			case "1": return "Invalid credentials.";
			default: return string.Empty;
			}
		}
	}
}
