// See https://aka.ms/new-console-template for more information
using System.Net;

var start = int.Parse(args[0]);

var client = new HttpClient();

for (int i = start; i < 241502; i++)
{
	Console.WriteLine("##### Fetching {0:X} ({0})", i);

	try
	{
		var response = await client.GetAsync($"https://splamy.de/api/ramses/mi/{i}");
		var content = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode != HttpStatusCode.NotFound)
			{
				await File.AppendAllTextAsync("error.log", $"{i:X}: {response.StatusCode} {content}\n");
			}

			Console.WriteLine($"##### Failed to fetch {i:X} ({i}) {response.StatusCode} {content}");
			continue;
		}

		Console.WriteLine(content);
		//await Task.Delay(100);
	}
	catch (Exception e)
	{
		await File.AppendAllTextAsync("error.log", $"{i:X}: {e.Message}\n");
		Console.WriteLine(e.Message);
	}
}
