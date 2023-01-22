using AutoMapper;
using AutoMapper.QueryableExtensions;
using Markdig;
using Markdig.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SplamyWeb.Components;
using SplamyWeb.Db;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
	private const int EntriesPerPage = 10;

	public ContentController(SplamyContext db, StoreService store, IMapper mapper, UserManager<LoginData> userManager)
	{
		this.db = db;
		this.store = store;
		this.mapper = mapper;
		this.userManager = userManager;
	}

	[AllowAnonymous]
	[HttpGet("home")]
	public async Task<BlogListQuery> GetHomePosts()
	{
		var tagName = await store.GetBlogMainTag();
		if (string.IsNullOrEmpty(tagName))
			return BlogListQuery.Empty;

		var isAdmin = await ExtendedPermission();

		IQueryable<BlogPostShortView> posts = (
			from post in db.BlogPosts.AsNoTracking()
			where post.Visible || isAdmin
			where post.Tags.Contains(tagName)
			select post)
			.ProjectTo<BlogPostShortView>(mapper.ConfigurationProvider);

		var result = await posts.ToListAsync();
		return new BlogListQuery()
		{
			Pages = 1,
			Posts = result,
		};
	}

	[AllowAnonymous]
	[HttpGet("posts")]
	public async Task<BlogListQuery> GetAllPosts([FromQuery] int? page = null)
	{
		var isAdmin = await ExtendedPermission();

		IQueryable<BlogPostShortView> posts = (
			from post in db.BlogPosts.AsNoTracking()
			where post.Visible || isAdmin
			orderby post.CreateTime descending
			select post)
			.ProjectTo<BlogPostShortView>(mapper.ConfigurationProvider);

		if (page is { } offsetNum)
			posts = posts.Skip(offsetNum * EntriesPerPage);
		posts = posts.Take(EntriesPerPage);
		var postsCnt = await db.BlogPosts.Where(post => post.Visible || isAdmin).CountAsync();

		return new BlogListQuery()
		{
			Pages = (postsCnt + EntriesPerPage - 1) / EntriesPerPage,
			Posts = await posts.ToArrayAsync(),
		};
	}

	// TODO /search?tags=(a&b)|c?text=free_text
	// https://www.postgresql.org/docs/12/textsearch-intro.html
	// https://www.postgresql.org/docs/12/ddl-generated-columns.html

	[AllowAnonymous]
	[HttpGet("post/{id}")]
	public async Task<ActionResult<BlogItemQuery>> GetPostById(int id)
	{
		var isAdmin = await ExtendedPermission();

		BlogPostView? postView = await (
			from post in db.BlogPosts.AsNoTracking()
			where post.PostId == id
			where post.Visible || isAdmin
			select post)
			.ProjectTo<BlogPostView>(mapper.ConfigurationProvider)
			.FirstOrDefaultAsync();

		if (postView == null)
			return NotFound();

		var recentPosts = await db.BlogPosts.AsNoTracking()
			.OrderByDescending(p => p.CreateTime)
			.Where(p => p.Visible && p.PostId != postView.PostId)
			.Take(3)
			.ProjectTo<BlogPostShortView>(mapper.ConfigurationProvider)
			.ToListAsync();

		return new BlogItemQuery
		{
			Post = postView,
			RecentPosts = recentPosts,
		};
	}

	[HttpGet("post/{id}/raw")]
	public async Task<ActionResult<BlogPostUpdate>> GetEditablePostById(int id)
	{
		BlogPostUpdate? postView = await (
			from post in db.BlogPosts.AsNoTracking()
			where post.PostId == id
			select post)
			.ProjectTo<BlogPostUpdate>(mapper.ConfigurationProvider)
			.FirstOrDefaultAsync();

		if (postView == null)
			return NotFound();
		return postView;
	}

	[HttpPut("post")]
	public async Task<ActionResult<BlogPostUpdate>> SaveOrUpdatePost([FromBody] BlogPostUpdate blogPostUpdate)
	{
		BlogPost? blogPost;
		bool wasVisible;
		if (blogPostUpdate.PostId is { } postId)
		{
			blogPost = await db.BlogPosts.FindAsync(postId);
			if (blogPost is null)
				return BadRequest("Post to update not found");
			wasVisible = blogPost.Visible;
		}
		else
		{
			blogPost = new()
			{
				PostId = default,
				Visible = true,
				CreateTime = DateTime.UtcNow,
			};
			await db.BlogPosts.AddAsync(blogPost);
			wasVisible = true;
		}

		if (blogPostUpdate.Visible is { } visible) blogPost.Visible = visible;
		if (blogPostUpdate.ContentRaw is not null) blogPost.ContentRaw = blogPostUpdate.ContentRaw;
		if (blogPostUpdate.Tags is not null) blogPost.Tags = blogPostUpdate.Tags;
		// If a post is published the first time set the create time
		if (!wasVisible && blogPost.Visible) blogPost.CreateTime = DateTime.UtcNow;

		TransformPostData(blogPost);

		await db.SaveChangesAsync();

		var updated = new BlogPostUpdate()
		{
			PostId = blogPost.PostId
		};
		return new JsonResult(updated, Util.JsonWebHideNull);
	}

	[HttpDelete("post/{id}")]
	public async Task<IActionResult> DeletePostById(int id)
	{
		var post = await db.BlogPosts.FindAsync(id);
		if (post is null)
			return NotFound();
		db.BlogPosts.Remove(post);
		await db.SaveChangesAsync();
		return Ok();
	}

	[HttpGet("tags")]
	public async Task<ActionResult<IList<string>>> GetAllTags()
	{
		var tags = await db.Set<string>().FromSqlRaw("SELECT DISTINCT UNNEST(b.\"Tags\") FROM blog b;").ToListAsync();
		return tags;
	}

	[AllowAnonymous]
	[HttpGet("feed/rss")]
	[Produces("text/xml")]
	public async Task<ActionResult<RssFeed>> GetFeedRss()
	{
		var posts = await (
			from post in db.BlogPosts.AsNoTracking()
			where post.Visible
			orderby post.CreateTime descending
			select post)
			.ProjectTo<BlogPostView>(mapper.ConfigurationProvider)
			.Take(EntriesPerPage)
			.ToArrayAsync();

		return new RssFeed
		{
			Channel = {
				Title = "Splamy's Blog",
				Link = "https://splamy.de/blog",
				Description = "Writing about random programming stuff.",
				Language = "en",
				PublishDate = "Wed, 22 Dec 2021 13:06:18 GMT",
				LastBuildDate = DateTime.UtcNow.ToString("r"),
				Items = posts.Select(p => new RssItem
				{
					Title = p.Title,
					Description = p.ContentHtml,
					Link = $"https://splamy.de/blog/post?i={p.PostId}",
					PublishDate = p.CreateTime.ToString("r"),
					Guid = $"https://splamy.de/blog/post?i={p.PostId}",
				}).ToArray()
			}
		};
	}

	private async ValueTask<IQueryable<BlogPost>> VisiblePosts()
	{
		var isAdmin = await ExtendedPermission();

		var query = db.BlogPosts.AsNoTracking();
		if (!isAdmin)
			query = query.Where(p => p.Visible);
		return query;
	}

	private static void TransformPostData(BlogPost post)
	{
		var doc = Markdown.Parse(post.ContentRaw);
		var parseState = 0;
		var title = "";

		using var summarySw = new StringWriter();
		var summaryRenderer = new Markdig.Renderers.HtmlRenderer(summarySw);

		foreach (var block in doc)
		{
			switch (parseState)
			{
			case 0:
				{
					if (block is HeadingBlock heading && heading.Level == 1)
					{
						parseState = 1;
						using var titleSw = new StringWriter();
						var titleRenderer = new Markdig.Renderers.HtmlRenderer(titleSw)
						{
							EnableHtmlForBlock = false,
							EnableHtmlForInline = false,
							EnableHtmlEscape = false
						};
						titleRenderer.Write(heading);
						titleRenderer.Writer.Flush();
						title = titleSw.ToString();
					}
					break;
				}

			case 1:
				{
					if (block is HeadingBlock heading)
					{
						parseState = 2;
					}
					else
					{

						summaryRenderer.Render(block);
					}
					break;
				}

			default:
				break;
			}
		}

		summaryRenderer.Writer.Flush();
		post.Title = title.Replace("\n", "");
		post.SummaryHtml = summarySw.ToString().Replace("\n", "");
		post.ContentHtml = doc.ToHtml();
	}

	private async ValueTask<bool> ExtendedPermission()
	{
		if (User.Identity?.IsAuthenticated != true)
			return false;
		var user = await userManager.GetUserAsync(User);
		if (user is null)
			return false;

		return user.Rank.AtLeast(UserType.Admin);
	}

	public class BlogListQuery
	{
		public int Pages { get; init; }
		public ICollection<BlogPostShortView>? Posts { get; init; }
		//public IList<BlogPostShortView>? RecentPosts { get; init; }

		public static BlogListQuery Empty { get; } = new() { Pages = 0, Posts = Array.Empty<BlogPostShortView>() };
	}

	public class BlogItemQuery
	{
		public BlogPostView Post { get; init; }
		public IList<BlogPostShortView>? RecentPosts { get; init; }
	}

	[XmlRoot("rss")]
	public class RssFeed
	{
		[XmlElement("channel")]
		public RssChannel Channel { get; set; } = new RssChannel();

		[XmlAttribute("version")]
		public string Version { get; set; } = "2.0";
	}

	public class RssChannel
	{
		[XmlElement("title")]
		public string Title { get; set; } = "";
		[XmlElement("link")]
		public string Link { get; set; } = "";
		[XmlElement("description")]
		public string Description { get; set; } = "";

		[XmlElement("language")]
		public string? Language { get; set; }
		[XmlElement("pubDate")]
		public string? PublishDate { get; set; }
		[XmlElement("lastBuildDate")]
		public string? LastBuildDate { get; set; }

		[XmlElement("item")]
		public RssItem[] Items { get; set; } = Array.Empty<RssItem>();
	}

	public class RssItem
	{
		[XmlElement("title")]
		public string Title { get; set; } = "";
		[XmlElement("link")]
		public string Link { get; set; } = "";
		[XmlElement("description")]
		public string Description { get; set; } = "";

		[XmlElement("pubDate")]
		public string? PublishDate { get; set; }
		[XmlElement("guid")]
		public string? Guid { get; set; }
	}
}
