namespace BomberBird.Pods
{
	/// <summary>
	/// Which piece of a burst covers a cell. Arms are built from these three drawings:
	/// the arm is authored horizontal and the end is authored pointing right, and both are
	/// rotated in engine for the other directions.
	/// </summary>
	public enum eBurstPiece
	{
		Centre,
		Arm,
		End,
	}
}
