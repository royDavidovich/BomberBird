namespace BomberBird.Flow
{
	/// <summary>
	/// The screen the run goes to next. <see cref="RunState"/> decides it and
	/// <see cref="GameFlow"/> loads it, so the rule about when the habitat card is shown can be
	/// tested without loading a scene.
	/// </summary>
	public enum eStageRoute
	{
		/// <summary>Arriving at a stage: its habitat card first, then the arena.</summary>
		HabitatCard,

		/// <summary>The same stage again, straight into the arena.</summary>
		Arena,

		/// <summary>Bird selection, before an arrival or before another attempt.</summary>
		BirdSelect,

		/// <summary>The campaign is over.</summary>
		Closing
	}
}
