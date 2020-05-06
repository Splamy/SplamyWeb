using Microsoft.AspNetCore.Mvc.RazorPages;
using SplamyWeb.Components;

namespace SplamyWeb.Pages.Admin
{
	public class StoreModel : PageModel
	{
		public StoreService Store { get; }

		public StoreModel(StoreService store)
		{
			this.Store = store;
		}

		public void OnGet()
		{

		}
	}
}
