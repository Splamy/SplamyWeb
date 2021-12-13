using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SplamyWeb.Components;

[Authorize]
public class MarkdownService : Hub
{
	public string Render(string? md) => string.IsNullOrEmpty(md) ? "" : Markdig.Markdown.ToHtml(md);
}
