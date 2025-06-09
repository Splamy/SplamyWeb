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
			var cols = lines[i].Replace("_", " _ ").Split([' '], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			for (int j = 0; j < cols.Length; j++)
			{
				var pos = RamsesNote.PosFromLoc(j, i);
				var col = cols[j];

				if (col is "_") continue;

				var type = col switch
				{
					"ru" => RamsesNoteType.RedUp,
					"rd" => RamsesNoteType.RedDown,
					"rl" => RamsesNoteType.RedLeft,
					"rr" => RamsesNoteType.RedRight,
					"rul" or "rlu" => RamsesNoteType.RedUpLeft,
					"rur" or "rru" => RamsesNoteType.RedUpRight,
					"rdl" or "rld" => RamsesNoteType.RedDownLeft,
					"rdr" or "rrd" => RamsesNoteType.RedDownRight,
					"rx" => RamsesNoteType.RedDot,

					"bu" => RamsesNoteType.BlueUp,
					"bd" => RamsesNoteType.BlueDown,
					"bl" => RamsesNoteType.BlueLeft,
					"br" => RamsesNoteType.BlueRight,
					"bul" or "blu" => RamsesNoteType.BlueUpLeft,
					"bur" or "bru" => RamsesNoteType.BlueUpRight,
					"bdl" or "bld" => RamsesNoteType.BlueDownLeft,
					"bdr" or "brd" => RamsesNoteType.BlueDownRight,
					"bx" => RamsesNoteType.BlueDot,

					"*" => RamsesNoteType.Bomb,

					_ => throw new ArgumentException("Invalid row element: " + col)
				};

				blocks.Add(new RamsesNote(type, pos));
			}
		}

		return new RamsesNoteFrame([.. blocks.OrderBy(x => x.Pos)]);
	}
}
