using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
	private readonly UserManager<LoginData> userManager;
	private readonly SplamyContext db;
	private readonly StoreService store;
	private readonly IMapper mapper;

	public ContentController(SplamyContext db, StoreService store, IMapper mapper, UserManager<LoginData> userManager)
	{
		this.db = db;
		this.store = store;
		this.mapper = mapper;
		this.userManager = userManager;
	}

	[AllowAnonymous]
	[HttpGet("home")]
	public async Task<IList<BlogPostView>> GetHomePosts()
	{
		var tagName = await store.GetBlogMainTag();
		if (string.IsNullOrEmpty(tagName))
			return Array.Empty<BlogPostView>();

		IQueryable<BlogPostView> posts = (
			from post in db.BlogPosts.AsNoTracking()
			where post.Tags.Contains(tagName)
			select post)
			.ProjectTo<BlogPostView>(mapper.ConfigurationProvider);

		var result = await posts.ToListAsync();
		return result;
	}

	[AllowAnonymous]
	[HttpGet("posts")]
	public async Task<IList<BlogPost>> GetAllPosts([FromQuery] int? offset = null)
	{
		IQueryable<BlogPost> posts =
			from post in db.BlogPosts.AsNoTracking()
			orderby post.PostId descending
			select post;

		if (offset is { } offsetNum)
			posts = posts.Skip(offsetNum);
		posts = posts.Take(10);

		return await posts.ToArrayAsync();
	}

	[AllowAnonymous]
	[HttpGet("post/{id}")]
	public async Task<IActionResult> GetPostById(int id)
	{
		var post = await db.BlogPosts.FindAsync(id);
		if (post == null)
			return NotFound();
		if (!post.Visible && !(await ExtendedPermission()))
			return NotFound();
		return Ok(post);
	}

	[HttpPut("post")]
	public async Task SaveOrUpdatePost([FromBody] BlogPost blogPost)
	{
		BlogPost? trackedPost = null;
		if (blogPost.PostId != 0)
		{
			trackedPost = await db.BlogPosts.FindAsync(blogPost.PostId);
		}

		if (trackedPost is not null)
		{
			mapper.Map(blogPost, trackedPost);
		}
		else
		{
			await db.BlogPosts.AddAsync(blogPost);
		}

		await db.SaveChangesAsync();
	}

	[HttpGet("tags")]
	public async Task<IActionResult> GetAllTags()
	{
		var tags = await db.Set<string>().FromSqlRaw("SELECT DISTINCT UNNEST(b.\"Tags\") FROM blog b;").ToListAsync();
		return Ok(tags);
	}

	// Tmp Helper

	[HttpPost("post/random")]
	public async Task PushRandomPost([FromQuery] string? tag = null)
	{
		string[] tags = tag is null ? Array.Empty<string>() : tag.Split(',', StringSplitOptions.TrimEntries);

		var blogPost = new BlogPost()
		{
			Visible = true,
			CreateTime = DateTime.UtcNow,
			Title = UserStore.RandomToken(12),
			ContentRaw = UserStore.RandomToken(100),
			ContentHtml = UserStore.RandomToken(105),
			Tags = tags,
		};

		await db.BlogPosts.AddAsync(blogPost);
		await db.SaveChangesAsync();
	}

	private async Task<bool> ExtendedPermission()
	{
		var user = await userManager.GetUserAsync(User);
		if (user is null)
			return false;

		return user.Rank.AtLeast(UserType.Admin);
	}
}
