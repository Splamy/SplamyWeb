using AutoMapper;
using AutoMapper.QueryableExtensions;
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
	private readonly IMapper mapper;

	public ContentController(SplamyContext db, StoreService store, IMapper mapper)
	{
		this.db = db;
		this.store = store;
		this.mapper = mapper;
	}

	[AllowAnonymous]
	[HttpGet("home")]
	public async Task<IList<BlogPostView>> GetProjects([FromQuery] int? offset = null)
	{
		var tagName = await store.GetBlogMainTag();
		if (string.IsNullOrEmpty(tagName))
			return Array.Empty<BlogPostView>();

		IQueryable<BlogPostView> posts = (
			from post in db.BlogPosts.AsNoTracking()
			where post.Tags.Contains(tagName)
			select post)
			.ProjectTo<BlogPostView>(mapper.ConfigurationProvider);

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
