using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;
using System.Linq;

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

		public string Error()
		{
			return (HttpContext.Request.Query["error"].FirstOrDefault()) switch
			{
				"1" => "Invalid credentials.",
				_ => string.Empty,
			};
		}
	}
}
