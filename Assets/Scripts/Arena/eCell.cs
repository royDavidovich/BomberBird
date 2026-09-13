namespace BomberBird.Arena
{
	/// <summary>What occupies a single arena cell.</summary>
	public enum eCell
	{
		Floor,
		Border,
		HardBlock,
		SoftBlock,

		/// <summary>
		/// Holds a stage's feather. Solid and indestructible like a hard block, and opened
		/// by the objective rather than by anything the player can aim at it.
		/// </summary>
		Cage,
	}
}
