namespace BomberBird.Flow
{
	/// <summary>How a stage ended, for the screen that reports it to the player.</summary>
	public enum eStageOutcome
	{
		/// <summary>The gate was reached. The run carries on.</summary>
		Cleared,

		/// <summary>
		/// The last life was spent. The run is not restarted: the player chooses between
		/// another attempt at this stage and returning to the menu.
		/// </summary>
		Failed,
	}
}
