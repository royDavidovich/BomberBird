namespace BomberBird.Flow
{
	/// <summary>
	/// When a stage's exit may be used, and when its feather is due to appear.
	///
	/// The objective is to defeat every common myna. On a stage that awards a feather, the
	/// last myna's death drops it and the exit stays shut until the player has walked over
	/// it, so the unlock is something earned rather than something announced. The intro and
	/// boss stages award none, and their exit opens the moment the arena is clear.
	///
	/// Pure functions with no state and no scene, so every combination can be checked in a
	/// test rather than played into.
	/// </summary>
	public static class StageGate
	{
		/// <summary>
		/// Whether the bird may leave. A feather that has been dropped but not picked up
		/// holds the exit shut, which is the whole point of dropping it.
		/// </summary>
		public static bool IsExitOpen(int i_LivingMynas, bool i_AwardsFeather, bool i_FeatherCollected)
		{
			return i_LivingMynas <= 0 && (!i_AwardsFeather || i_FeatherCollected);
		}

		/// <summary>
		/// Whether the feather should be put on the arena now. False once it is already
		/// there, so a stage drops exactly one.
		/// </summary>
		public static bool IsFeatherDue(int i_LivingMynas, bool i_AwardsFeather, bool i_FeatherDropped)
		{
			return i_LivingMynas <= 0 && i_AwardsFeather && !i_FeatherDropped;
		}
	}
}
