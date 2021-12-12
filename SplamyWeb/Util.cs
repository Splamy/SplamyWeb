global using System;
global using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

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
		Converters = { new TimeSpanConverter(TimeSpanFormatting.ToString) },
	};
	public static readonly JsonSerializerOptions JsonWebHideNull = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new TimeSpanConverter(TimeSpanFormatting.ToString) },
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

internal class TimeSpanConverter : JsonConverter<TimeSpan>
{
	private readonly TimeSpanFormatting format;

	public TimeSpanConverter(TimeSpanFormatting format)
	{
		this.format = format;
	}

	public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
		case JsonTokenType.String:
		case JsonTokenType.Null:
			var str = reader.GetString() ?? throw new JsonException("TimeSpan value is empty");
			return ParseTime(str) ?? throw new JsonException("Invalid TimeSpan");
		case JsonTokenType.Number:
			var secs = reader.GetDouble();
			return TimeSpan.FromSeconds(secs);
		case JsonTokenType.StartObject:
			var helper = JsonSerializer.Deserialize<TickReaderHelper>(ref reader);
			return TimeSpan.FromTicks(helper.Ticks);
		default:
			throw new JsonException($"Invalid token type '{reader.TokenType}' for TimeSpan");
		}
	}

	public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
	{
		switch (format)
		{
		case TimeSpanFormatting.Simple:
			throw new NotSupportedException();
		case TimeSpanFormatting.Seconds:
			writer.WriteNumberValue(value.TotalSeconds);
			break;
		case TimeSpanFormatting.Xml:
			writer.WriteStringValue(XmlConvert.ToString(value));
			break;
		case TimeSpanFormatting.ToString:
			writer.WriteStringValue(value.ToString("c", CultureInfo.InvariantCulture));
			break;
		case var _unhandled:
			throw new ArgumentOutOfRangeException(_unhandled.ToString());
		}
	}

	public static TimeSpan? ParseTime(string value)
	{
		if (value is null) return null;
		return ParseTimeAsSimple(value)
			?? ParseTimeAsDigital(value)
			?? ParseTimeAsXml(value);
	}

	private static readonly Regex TimeReg = new(@"^(?:(\d+)d)?(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?(?:(\d+)ms)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ECMAScript);

	private static TimeSpan? ParseTimeAsSimple(string value)
	{
		static int AsNum(string svalue)
		{
			if (string.IsNullOrEmpty(svalue))
				return 0;
			return int.TryParse(svalue, out var num) ? num : 0;
		}

		var match = TimeReg.Match(value);
		if (match.Success)
		{
			try
			{
				return new TimeSpan(
					AsNum(match.Groups[1].Value),
					AsNum(match.Groups[2].Value),
					AsNum(match.Groups[3].Value),
					AsNum(match.Groups[4].Value),
					AsNum(match.Groups[5].Value));
			}
			catch { }
		}
		return null;
	}

	private static TimeSpan? ParseTimeAsDigital(string value)
	{
		if (value.Contains(':'))
		{
			string[] splittime = value.Split(':');

			if (splittime.Length == 2
				&& int.TryParse(splittime[0], out var minutes)
				&& double.TryParse(splittime[1], NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
			{
				return TimeSpan.FromSeconds(seconds) + TimeSpan.FromMinutes(minutes);
			}
		}
		else
		{
			if (double.TryParse(value, NumberStyles.Integer | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
				return TimeSpan.FromSeconds(seconds);
		}
		return null;
	}

	private static TimeSpan? ParseTimeAsXml(string value)
	{
		try { return XmlConvert.ToTimeSpan(value); }
		catch (FormatException) { return null; }
	}


	private struct TickReaderHelper
	{
		public long Ticks { get; set; }
	}
}

enum TimeSpanFormatting
{
	Simple,
	Seconds,
	Xml,
	ToString,
}
