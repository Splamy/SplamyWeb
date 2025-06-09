using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace SplamyWeb.Components.Ramses;

public readonly record struct RamsesNoteFrame(ImmutableArray<RamsesNote> Blocks)
{
	static readonly string Alphabet = new([
		.. Enumerable.Range(0, 26).Select(i => (char)('a' + i)),
		//.. Enumerable.Range(0, 26).Select(i => (char)('A' + i)),
		.. Enumerable.Range(0, 10).Select(i => (char)('0' + i)),
	]);

	public string ToWord()
	{
		var word = DecimalToBase(ToSpaceOptNumber(), (uint)Alphabet.Length);
		return word;
	}

	public BigInteger ToSpaceOptNumber()
	{
		// 1 Blocks ->     0 -      228 (19 * 12) ** 1 + 0
		// 2 Blocks ->   228 -    52212 (19 * 12) ** 2 + 228
		// 3 Blocks -> 52212 - 11904564 (19 * 12) ** 3 + 52212

		BigInteger offset = 0;
		BigInteger number = 0;
		for (int i = 0; i < Blocks.Length; i++)
		{
			offset += BigInteger.Pow(RamsesNote.ItemVariations, i + 1);
			number = number * RamsesNote.ItemVariationsBI + (BigInteger)Blocks[i].ToNumber();
		}

		return number + offset;
	}

	public static string DecimalToBase(BigInteger decimalNumber, uint radix)
	{
		if (radix < 2 || radix > Alphabet.Length)
			throw new ArgumentException("The radix must be >= 2 and <= " + Alphabet.Length.ToString());

		if (decimalNumber == 0)
			return Alphabet[0..1];

		int index = 0;
		int BitsInLong = (int)decimalNumber.GetBitLength();
		char[] charArray = new char[BitsInLong];

		while (decimalNumber != 0)
		{
			var remainder = (uint)(decimalNumber % radix);
			charArray[index++] = Alphabet[(int)remainder];
			decimalNumber = decimalNumber / radix;
		}

		return new string(charArray, 0, index);
	}

	public static RamsesNoteFrame ParseRadableFrame(string text)
	{
		var lines = text.ToLowerInvariant().Split('/');
		if (lines.Length != 3)
			throw new ArgumentException("Invalid frame format, expected 3 lines separated by '/'");

		var blocks = new List<RamsesNote>();
		for (int i = 0; i < lines.Length; i++)
		{
			var cols = lines[i].Split(' ');

			for (int j = 0; j < cols.Length; j++)
			{
				var pos = RamsesNote.PosFromLoc(j, i);

				if (cols[j] is [char c])
				{
					blocks.Add(c switch
					{
						'*' => new RamsesNote(RamsesNoteType.Bomb, pos),
					});
				}
				else if (cols[j] is [char c1, .. string c2])
				{
					var color = c1 switch
					{
						'r' => RamsesNoteType.RedUp,
						'b' => RamsesNoteType.BlueUp,
						_ => throw new ArgumentException("Invalid column character: " + c1)
					};
					var direction = c2 switch
					{
						"u" or "up" => RamsesNoteType.RedUp,
						"d" or "do" => RamsesNoteType.RedDown,
						"l" or "le" => RamsesNoteType.RedLeft,
						"r" or "ri" => RamsesNoteType.RedRight,
						"ul" or "lu" => RamsesNoteType.RedUpLeft,
						"ur" or "ru" => RamsesNoteType.RedUpRight,
						"dl" or "ld" => RamsesNoteType.RedDownLeft,
						"dr" or "rd" => RamsesNoteType.RedDownRight,
						"x" => RamsesNoteType.RedDot,
						_ => throw new ArgumentException("Invalid row character: " + c2)
					};

					var final = (RamsesNoteType)((int)direction + (color is RamsesNoteType.RedUp ? 0 : 9));

					blocks.Add(new RamsesNote(final, pos));
				}

			}
		}

		return new RamsesNoteFrame([.. blocks.OrderBy(x => x.Pos)]);
	}
}
