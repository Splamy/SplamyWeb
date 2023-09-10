using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using static SplamyWeb.Util;
using Microsoft.Extensions.Options;
using SplamyWeb.Modules;
using System.IO;
using System.Linq;
using System.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace SplamyWeb.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthScheme)]
[Route("api/[controller]")]
public class WireguardController : ControllerBase
{
	private readonly IOptionsMonitor<WireguardConfig> config;

	public WireguardController(IOptionsMonitor<WireguardConfig> config)
	{
		this.config = config;
	}

	[HttpGet("peers")]
	public Task<IActionResult> GetPeers() => Transfrom((data) =>
	{
		var dtos = data.File.Sections
			.Where(s => s.Name == "WireGuardPeer")
			.Select(FromSection)
			.Where(dto => dto != null)
			.ToList();
		return Ok(dtos);
	});

	[HttpPut("peers")]
	public Task<IActionResult> AddPeerAsync([FromBody] WireguardPeerDto dto) => Transfrom((data) =>
	{
		var peerSection = data.File.Sections
			.FirstOrDefault(s => s.Name == "WireGuardPeer" && s.Entries.OfType<IniValue>().Any(e => e.Key == "PublicKey" && e.Value == dto.PublicKey));

		if (peerSection == null)
		{
			peerSection = ToSection(dto);
			data.File.Sections.Add(peerSection);
		}
		else
		{
			var tmpSection = ToSection(dto);
			peerSection.Entries.Clear();
			peerSection.Entries.AddRange(tmpSection.Entries);
		}

		data.Changed = true;
		return Ok();
	});

	[HttpDelete("peers")]
	public Task<IActionResult> RemovePeerAsync([FromBody] string publicKey) => Transfrom((data) =>
	{
		var peerSection = data.File.Sections
			.FirstOrDefault(s => s.Name == "WireGuardPeer" && s.Entries.OfType<IniValue>().Any(e => e.Key == "PublicKey" && e.Value == publicKey));

		if (peerSection == null)
		{
			return NotFound();
		}

		data.File.Sections.Remove(peerSection);
		data.Changed = true;

		return Ok();
	});

	private static WireguardPeerDto? FromSection(IniSection section)
	{
		var publicKey = section.Entries.OfType<IniValue>().FirstOrDefault(e => e.Key == "PublicKey")?.Value;
		if (publicKey == null)
		{
			return null;
		}
		var friendlyName = section.Entries.OfType<IniComment>()
			.Select(c => c.Comment.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) is [var key, var value] ? (key, value) : (null, null))
			.Where(t => t.key == "friendly_name")
			.Select(t => t.value!)
			.FirstOrDefault();
		return new WireguardPeerDto
		{
			FriendlyName = friendlyName,
			PublicKey = publicKey,
			AllowedIPs = section.Entries.OfType<IniValue>().Where(e => e.Key == "AllowedIPs").Select(e => e.Value).ToList(),
		};
	}

	private static IniSection ToSection(WireguardPeerDto dto)
	{
		var peerSection = new IniSection
		{
			Name = "WireGuardPeer",
			Entries =
			{
				new IniComment($"friendly_name = {dto.FriendlyName}"),
				new IniValue("PublicKey", dto.PublicKey!)
			}
		};
		foreach (var allowedIP in dto.AllowedIPs)
		{
			peerSection.Entries.Add(new IniValue("AllowedIPs", allowedIP));
		}
		return peerSection;
	}

	private async Task<IActionResult> Transfrom(Func<TransformAction, IActionResult> action)
	{
		await using var fs = new FileStream(config.CurrentValue.Path, FileMode.Open, FileAccess.ReadWrite);
		var initData = IniFile.Parse(fs);

		var transformAction = new TransformAction { File = initData };
		var result = action(transformAction);

		if (transformAction.Changed)
		{
			fs.Seek(0, SeekOrigin.Begin);
			fs.SetLength(0);
			initData.Write(fs);
		}

		return result;
	}

	class TransformAction
	{
		public required IniFile File { get; init; }
		public bool Changed { get; set; }
	}
}

public class WireguardConfig
{
	public required string Path { get; set; }
}

public class WireguardPeerDto : IEquatable<WireguardPeerDto>
{
	[RegularExpression(@"^[a-zA-Z0-9+/]{43}=$")]
	public required string PublicKey { get; set; }
	[RegularExpression(@"^[a-zA-Z0-9_\- ]{0,32}$")]
	public string? FriendlyName { get; set; }
	public List<string> AllowedIPs { get; set; } = new();

	public bool Equals(WireguardPeerDto? other) => other != null &&
		PublicKey == other.PublicKey &&
		FriendlyName == other.FriendlyName &&
		AllowedIPs.SequenceEqual(other.AllowedIPs);

	public override bool Equals(object obj) => Equals(obj as WireguardPeerDto);
}
