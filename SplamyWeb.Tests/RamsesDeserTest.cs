using SplamyWeb.Components;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static SplamyWeb.Components.RamsesService;

namespace SplamyWeb.Tests;

public class RamsesDeserTest
{
	const string TestJson1 = """
		{
			"type": "MAP_UPDATE",
			"msg": {
				"id": "42b6f",
				"name": "PINK FLUFFY FUCKING UNICORNS DANCING ON RAINBOWS",
				"description": "",
				"uploader": {
					"id": 251342,
					"name": "FOBOS",
					"avatar": "https://cdn.beatsaver.com/avatar/251342.jpg",
					"type": "SIMPLE",
					"admin": false,
					"curator": false,
					"seniorCurator": false,
					"playlistUrl": "https://api.beatsaver.com/users/id/251342/playlist"
				},
				"metadata": {
					"bpm": 118.0,
					"duration": 92,
					"songName": "PINK FLUFFY FUCKING UNICORNS",
					"songSubName": "",
					"songAuthorName": "Yaplap (original by Andrew Huang)",
					"levelAuthorName": "FOBOS"
				},
				"stats": {
					"plays": 0,
					"downloads": 0,
					"upvotes": 0,
					"downvotes": 0,
					"score": 0.5
				},
				"uploaded": "2024-12-28T03:40:42.067839Z",
				"automapper": false,
				"ranked": false,
				"qualified": false,
				"versions": [
					{
						"hash": "3131eb1ad25251f87665697b20e45c1cd4e06991",
						"state": "Published",
						"createdAt": "2024-12-28T03:37:48.629715Z",
						"sageScore": 3,
						"diffs": [
							{
								"njs": 10.0,
								"offset": 0.0,
								"notes": 367,
								"bombs": 24,
								"obstacles": 98,
								"nps": 4.101,
								"length": 176.0,
								"characteristic": "Standard",
								"difficulty": "Hard",
								"events": 950,
								"chroma": false,
								"me": false,
								"ne": false,
								"cinema": false,
								"seconds": 89.492,
								"paritySummary": {
									"errors": 32,
									"warns": 5,
									"resets": 0
								},
								"maxScore": 330395,
								"environment": "DefaultEnvironment"
							}
						],
						"downloadURL": "https://cdn.beatsaver.com/3131eb1ad25251f87665697b20e45c1cd4e06991.zip",
						"coverURL": "https://cdn.beatsaver.com/3131eb1ad25251f87665697b20e45c1cd4e06991.jpg",
						"previewURL": "https://cdn.beatsaver.com/3131eb1ad25251f87665697b20e45c1cd4e06991.mp3"
					}
				],
				"createdAt": "2024-12-28T03:37:48.629715Z",
				"updatedAt": "2024-12-28T03:40:42.067839Z",
				"lastPublishedAt": "2024-12-28T03:40:42.067839Z",
				"tags": [
					"comedy-meme",
					"challenge",
					"fitness"
				],
				"declaredAi": "None",
				"blRanked": false,
				"blQualified": false
			}
		}
		""";

	const string TestJson2 = """
		{
			"type": "MAP_DELETE",
			"msg": "42b6f"
		}
		""";

	[Fact]
	public void TestJsonDeser1()
	{
		JsonSerializer.Deserialize<BsMessageBase>(TestJson1, RamsesService.JsonSerializerOptions);
	}

	[Fact]
	public void TestJsonDeser2()
	{
		JsonSerializer.Deserialize<BsMessageBase>(TestJson2, RamsesService.JsonSerializerOptions);
	}

	[Fact]
	public async Task TestJsonDeser1_2()
	{
		using var memStream =
			new System.IO.MemoryStream(Encoding.UTF8.GetBytes("[" + TestJson1 + "," + TestJson2 + "]"));

		var enu = JsonSerializer.DeserializeAsyncEnumerable<BsMessageBase>(memStream,
			RamsesService.JsonSerializerOptions, TestContext.Current.CancellationToken);

		int count = 0;
		await foreach (var msg in enu)
		{
			Assert.NotNull(msg);
			count++;
		}

		Assert.Equal(2, count);
	}
}
