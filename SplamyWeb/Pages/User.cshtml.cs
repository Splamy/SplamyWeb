using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SplamyWeb.Pages
{
	public class UserModel : PageModel
	{
		public void OnGet()
		{
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