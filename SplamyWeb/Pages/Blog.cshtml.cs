using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;

namespace SplamyWeb.Pages
{
	public class BlogModel : PageModel
	{
		// Features:
		// Create new (POST/PUT)
		// Edit (PATCH)
		// Delete (DELETE)

		// Optional:
		// Hide

		// Requirements
		// Edit Field (+opt preview field)

		public void OnGet()
		{
		}

		public IEnumerable<BlogEntry> GetBlogs()
		{
			yield return new BlogEntry
			{
				Header = "Hi",
				Summary = "This is blog",
				Tags = new[] { "ts3audiobot" },
			};
			yield break;
		}
	}

	public class BlogEntry
	{
		public string? Header { get; set; }
		public string? Summary { get; set; }
		public string[] Tags { get; set; } = Array.Empty<string>();
		public string? ContentRaw { get; set; }
		public string? ContentHtml { get; set; }
	}
}
