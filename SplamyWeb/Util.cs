using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SplamyWeb
{
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

		public static readonly NLog.Targets.MemoryTarget NLogMemory = new()
		{
			Layout = "${longdate} | ${level} | ${message}",
		};

		private static readonly Regex saveRegex = new(@"^[\w-_]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

		public static readonly JsonSerializerOptions JsonDefault = new()
		{
			Converters = { new TimeSpanConverter() },
		};

		public static bool IsSave(string param) => saveRegex.IsMatch(param);

		public static string Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
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
}
