using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SplamyWeb
{
	public static class Util
	{
		public const string AuthScheme = "BasicAuthentication,Identity.Application";

		public static readonly HttpClient httpClient = new HttpClient();

		static Util()
		{
			httpClient.DefaultRequestHeaders.UserAgent.Clear();
			httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SplamyWeb", "1.0.0"));
		}

		public static NLog.Targets.MemoryTarget NLogMemory = new NLog.Targets.MemoryTarget()
		{
			Layout = "${longdate} | ${level} | ${message}",
		};

		private static readonly Regex saveRegex = new Regex(@"^[\w-_]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ECMAScript);

		public static JsonSerializerOptions JsonDefault = new JsonSerializerOptions()
		{
			Converters = { new TimeSpanConverter() },
		};

		public static bool IsSave(string param) => saveRegex.IsMatch(param);

		public static string Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		public static TimeSpan Sum(this IEnumerable<TimeSpan?> source)
		{
			TimeSpan sum = TimeSpan.Zero;
			foreach (var v in source)
				if (v != null)
					sum += v.GetValueOrDefault();
			return sum;
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
			return TimeSpan.Parse(reader.GetString());
		}

		public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}
