using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.Linq;
using System.Threading.Tasks;

namespace SplamyWeb.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = Util.AuthScheme)]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
	private readonly SplamyContext db;
	private readonly StoreService store;

	public ContentController(SplamyContext db, StoreService store)
	{
		this.db = db;
		this.store = store;
	}

	[AllowAnonymous]
	[HttpGet("home")]
	public async Task<IList<PortfolioEntry>> GetProjects([FromQuery] int? offset = null)
	{
		var tagName = await store.GetBlogMainTag();
		if (string.IsNullOrEmpty(tagName))
			return Array.Empty<PortfolioEntry>();

		IQueryable<PortfolioEntry> posts =
			from post in db.BlogPosts.AsNoTracking()
			where post.Tags.Contains(tagName)
			select new PortfolioEntry()
			{
				Title = post.Title,
				ContentHtml = post.ContentHtml,
				Tags = post.Tags,
			};

		if (offset is { } offsetNum)
			posts = posts.Skip(offsetNum);
		posts = posts.Take(10);

		var result = await posts.ToListAsync();
		return result;
	}

	[HttpPost("post/random")]
	public async Task PushRandomPost([FromQuery] string? tag = null)
	{
		string[] tags = tag is null ? Array.Empty<string>() : tag.Split(',', StringSplitOptions.TrimEntries);

		var blogPost = new BlogPost()
		{
			Visible = true,
			CreateTime = DateTime.UtcNow,
			Title = UserStore.RandomToken(12),
			Summary = UserStore.RandomToken(30),
			ContentRaw = UserStore.RandomToken(100),
			ContentHtml = UserStore.RandomToken(110),
			Tags = tags,
		};

		await db.BlogPosts.AddAsync(blogPost);
		await db.SaveChangesAsync();
	}

	[HttpGet("post/all")]
	public async Task<IList<BlogPost>> GetAllPosts()
	{
		IQueryable<BlogPost> posts =
			from post in db.BlogPosts.AsNoTracking()
			orderby post.PostId descending
			select post;

		return await posts.ToArrayAsync();
	}
}

public class PortfolioEntry
{
	public string Title { get; set; }
	public string ContentHtml { get; set; }
	public IList<string> Tags { get; set; }
}
