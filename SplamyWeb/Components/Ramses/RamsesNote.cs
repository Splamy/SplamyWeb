using RateMapSeveritySaber;
using System.Numerics;

namespace SplamyWeb.Components.Ramses;

public readonly record struct RamsesNote(RamsesNoteType Type, int Pos)
{

	public static readonly int CellVariations = Enum.GetValues<RamsesNoteType>().Length;
	public const int CellPositions = 4 * 3; // 4 columns, 3 rows 
	public static readonly int ItemVariations = CellVariations * CellPositions;
	public static readonly BigInteger ItemVariationsBI = new(ItemVariations);

	public static RamsesNote? TryFrom(JsonNote note)
	{
		if (Enum.IsDefined(note.Direction)
			&& Enum.IsDefined(note.Type)
			&& note.X is >= 0 and <= 3
			&& note.Y is >= 0 and <= 2)
		{
			return new RamsesNote((RamsesNoteType)((note.Type == NoteColor.Red ? 0 : 9) + note.Direction), PosFromLoc(note.X, note.Y));
		}
		return null;
	}

	public static int PosFromLoc(int x, int y)
	{
		if (x < 0 || x > 3 || y < 0 || y > 2)
			throw new ArgumentOutOfRangeException("Position out of bounds");
		return x + y * 4;
	}

	public readonly long ToNumber() => (long)Type + Pos * CellPositions;
}
