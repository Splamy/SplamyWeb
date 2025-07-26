using System.Diagnostics;
using System.IO;
using static SplamyWeb.Util;

namespace SplamyWeb.Modules;

[DebuggerDisplay("{Sections.Count} Sections")]
public class IniFile
{
	public List<IniSection> Sections { get; } = [];

	public static IniFile Parse(Stream stream)
	{
		using var reader = new StreamReader(stream, Utf8Encoding, leaveOpen: true);
		return Parse(reader);
	}

	public static IniFile Parse(TextReader reader)
	{
		var file = new IniFile();
		IniSection? section = null;
		while (reader.ReadLine() is { } line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			if (line.StartsWith('[') && line.EndsWith(']'))
			{
				section = new IniSection { Name = line[1..^1] };
				file.Sections.Add(section);
				continue;
			}

			if (section == null)
			{
				continue;
			}

			if (line.StartsWith('#'))
			{
				var comment = line[1..].TrimStart();
				section.Entries.Add(new IniComment(comment));
				continue;
			}

			if (line.Split('=', 2) is [var key, var value])
			{
				section.Entries.Add(new IniValue(key.Trim(), value.Trim()));
				continue;
			}

			section.Entries.Add(new IniUnparsable(line));
		}

		return file;
	}

	public void Write(Stream stream)
	{
		using var writer = new StreamWriter(stream, Utf8Encoding, leaveOpen: true);
		Write(writer);
	}

	public void Write(TextWriter writer)
	{
		foreach (var section in Sections)
		{
			writer.WriteLine($"[{section.Name}]");
			foreach (var entry in section.Entries)
			{
				switch (entry)
				{
				case IniComment comment:
					writer.WriteLine($"# {comment.Comment}");
					break;
				case IniValue value:
					writer.WriteLine($"{value.Key} = {value.Value}");
					break;
				case IniUnparsable unparsable:
					writer.WriteLine(unparsable.Line);
					break;
				}
			}
			writer.WriteLine();
		}
		writer.Flush();
	}
}

[DebuggerDisplay("[{Name,nq}] {Entries.Count} Entries")]
public class IniSection
{
	public required string Name { get; set; }
	public List<IniEntry> Entries { get; } = [];
}

public record IniEntry;
[DebuggerDisplay("{Key,nq} = {Value,nq}")]
public record IniValue(string Key, string Value) : IniEntry;
[DebuggerDisplay("{Comment,nq}")]
public record IniComment(string Comment) : IniEntry;
public record IniUnparsable(string Line) : IniEntry;
