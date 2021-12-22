using AutoMapper;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

public class BlogProfile : Profile
{
	public BlogProfile()
	{
		CreateMap<BlogPost, BlogPostShortView>(MemberList.Destination);
		CreateMap<BlogPost, BlogPostView>(MemberList.Destination);
		CreateMap<BlogPost, BlogPostUpdate>(MemberList.Destination);
	}
}


#pragma warning restore CS8618
