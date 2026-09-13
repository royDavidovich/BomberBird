namespace BomberBird.Player
{
	/// <summary>
	/// Everything <see cref="BirdAnimator"/> needs in order to pick a frame: which way the
	/// body faces, and whether it is actually travelling.
	///
	/// Extracted only once a second walker existed. The player's bird reads input and slides
	/// freely between cells; a myna decides for itself and steps cell to cell. Neither cares
	/// how the other moves, and both are drawn the same way.
	/// </summary>
	public interface IGridWalker
	{
		/// <summary>Which way the body is facing.</summary>
		eFacing Facing { get; }

		/// <summary>True while the body actually changed position this step.</summary>
		bool IsMoving { get; }
	}
}
