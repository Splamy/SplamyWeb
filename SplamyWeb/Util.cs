global using System;
global using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SplamyWeb;

public static class Util
{
	public const string AuthScheme = "BasicAuthentication,Identity.Application";

	public static readonly HttpClient httpClient = new();
	public static string DataPath { get; } = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "data"));
	public static readonly Encoding Utf8Encoding = new UTF8Encoding(false, false);

	static Util()
	{
		httpClient.DefaultRequestHeaders.UserAgent.Clear();
		httpClient.DefaultRequestHeaders.UserAgent.Add(new("SplamyWeb", "1.0.0"));
	}

	private static readonly Regex saveRegex = new(@"^[\w\d-_]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);
	public static readonly Regex fileCleanRegex = new(@"[^\w\d-_]", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);

	public static readonly JsonSerializerOptions JsonDefault = new()
	{
		Converters = { new TimeSpanConverter() },
	};
	public static readonly JsonSerializerOptions JsonWebHideNull = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public static bool IsSave(string param) => saveRegex.IsMatch(param);
	public static string CleanFilenameForPath(string name)
	{
		var clean = fileCleanRegex.Replace(name, "");
		if (clean.Contains('.') || clean.Contains('/') || clean.Contains('\\'))
		{
			throw new Exception($"This shouldn't happen. Source: <{name}> Clean: <{clean}>");
		}
		return clean;
	}

	public static string Truncate(this string value, int maxLength)
	{
		if (string.IsNullOrEmpty(value)) return value;
		return value.Length <= maxLength ? value : value[..maxLength];
	}

	public static uint Sum(this IEnumerable<uint> source)
	{
		uint sum = 0;
		foreach (var v in source)
			sum += v;
		return sum;
	}
	public static uint Sum(this IEnumerable<uint?> source)
	{
		uint sum = 0;
		foreach (var v in source)
			if (v != null)
				sum += v.GetValueOrDefault();
		return sum;
	}

	public static bool MatchPrefix(this string str, string prefix, [MaybeNullWhen(false)] out string rest)
		=> str.MatchPrefix(prefix, StringComparison.Ordinal, out rest);
	public static bool MatchPrefix(this string str, string prefix, StringComparison comparisonType, [MaybeNullWhen(false)] out string rest)
	{
		if (str.StartsWith(prefix, comparisonType))
		{
			rest = str[prefix.Length..];
			return true;
		}
		else
		{
			rest = default!;
			return false;
		}
	}
}

public class TimeSpanConverter : JsonConverter<TimeSpan>
{
	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var str = reader.GetString();
		if (string.IsNullOrEmpty(str))
			throw new FormatException("Expected timespan string but got nothing");
		return TimeSpan.Parse(str);
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
