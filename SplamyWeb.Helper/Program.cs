// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var client = new HttpClient();

for (int i = 12596; i < 241502; i++)
{
	Console.WriteLine("##### Fetching {0:X} ({0})", i);

	try
	{
		var response = await client.GetAsync($"https://splamy.de/api/ramses/mi/{i}");
		var content = await response.Content.ReadAsStringAsync();
		Console.WriteLine(content);
		await Task.Delay(100);
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
	}
}
