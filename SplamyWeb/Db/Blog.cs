using Riok.Mapperly.Abstractions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SplamyWeb.Db;

#pragma warning disable CS8618
[Table("blog")]
public class BlogPost
{
	[Key]
	public int PostId { get; set; }
	public bool Visible { get; set; }
	public DateTime CreateTime { get; set; }
	public string Title { get; set; }
	public string SummaryHtml { get; set; }
	public string ContentRaw { get; set; }
	public string ContentHtml { get; set; }

	// Important:
	// Gets a gin index via migration
	// http://www.databasesoup.com/2015/01/tag-all-things.html
	public string[] Tags { get; set; } = Array.Empty<string>();
}

public class BlogPostView : BlogPostShortView
{
	public string ContentHtml { get; set; }
}

public class BlogPostShortView
{
	public int PostId { get; set; }
	public bool? Visible { get; set; }
	public DateTime CreateTime { get; set; }
	public string Title { get; set; }
	public string SummaryHtml { get; set; }
	public string[] Tags { get; set; }
}

public class BlogPostUpdate
{
	public int? PostId { get; set; }
	public bool? Visible { get; set; }
	public string? ContentRaw { get; set; }
	public string[]? Tags { get; set; }
}

#pragma warning restore CS8618

[Mapper]
public static partial class BlogMapper
{
	public static IQueryable<BlogPost> IsVisible(this IQueryable<BlogPost> query, bool isAdmin) => isAdmin ? query : query.Where(post => post.Visible);

	[MapperIgnoreSource(nameof(BlogPost.ContentRaw))]
	public static partial BlogPostView ToView(BlogPost blogPost);

	[MapperIgnoreSource(nameof(BlogPost.ContentRaw))]
	public static partial IQueryable<BlogPostView> ProjectToView(this IQueryable<BlogPost> blogPost);

	[MapperIgnoreSource(nameof(BlogPost.ContentRaw))]
	[MapperIgnoreSource(nameof(BlogPost.ContentHtml))]
	public static partial BlogPostShortView ToShortView(BlogPost blogPost);

	[MapperIgnoreSource(nameof(BlogPost.ContentRaw))]
	[MapperIgnoreSource(nameof(BlogPost.ContentHtml))]
	public static partial IQueryable<BlogPostShortView> ProjectToShortView(this IQueryable<BlogPost> blogPost);

	[MapperIgnoreSource(nameof(BlogPost.CreateTime))]
	[MapperIgnoreSource(nameof(BlogPost.Title))]
	[MapperIgnoreSource(nameof(BlogPost.SummaryHtml))]
	[MapperIgnoreSource(nameof(BlogPost.ContentHtml))]
	public static partial BlogPostUpdate ToUpdate(BlogPost blogPostUpdate);

	[MapperIgnoreSource(nameof(BlogPost.CreateTime))]
	[MapperIgnoreSource(nameof(BlogPost.Title))]
	[MapperIgnoreSource(nameof(BlogPost.SummaryHtml))]
	[MapperIgnoreSource(nameof(BlogPost.ContentHtml))]
	public static partial IQueryable<BlogPostUpdate> ProjectToUpdate(this IQueryable<BlogPost> blogPostUpdate);
}
