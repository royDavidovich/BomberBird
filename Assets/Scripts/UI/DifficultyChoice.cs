namespace BomberBird.UI
{
	/// <summary>
	/// The difficulty ladder, as the menu's slider presents it: stop 0 is the easiest and the
	/// numbers climb to the right, which is the only order a slider can honestly mean.
	///
	/// It exists so the mapping is not spelled out inside a MonoBehaviour. The game stores the
	/// difficulty as one bool - <see cref="BomberBird.Flow.GameFlow.EasyMynas"/> - and a bool
	/// has no left and right, so something has to say which end of the slider it sits at. That
	/// something is here, where a test can reach it without a scene, rather than in the panel.
	///
	/// A third stop is expected. When Hard arrives this is the one file that decides what it
	/// means; the panel only has to raise the slider's maximum to match <see cref="StopCount"/>.
	/// The tuning behind a Hard myna is a separate decision and is deliberately not assumed here.
	/// </summary>
	public static class DifficultyChoice
	{
		/// <summary>How many stops the slider offers. Easy and Normal today.</summary>
		public const int StopCount = 2;

		private static readonly string[] sr_Names = { "Easy", "Normal", "Hard" };

		/// <summary>
		/// Whether the stop at this position means the easier flock.
		///
		/// Everything below Normal is easy, so the check is a threshold rather than an equality:
		/// adding Hard at stop 2 must not quietly turn Normal easy.
		/// </summary>
		public static bool IsEasyAt(int i_Stop)
		{
			return Clamp(i_Stop) == 0;
		}

		/// <summary>The name shown under this stop's notch.</summary>
		public static string NameAt(int i_Stop)
		{
			return sr_Names[Clamp(i_Stop)];
		}

		/// <summary>
		/// Which stop the slider opens on, given the setting the run is already carrying. The
		/// popup shows where the player left it rather than the authored default, because the
		/// preference outlives the sitting.
		/// </summary>
		public static int StopFor(bool i_EasyMynas)
		{
			return i_EasyMynas ? 0 : 1;
		}

		/// <summary>
		/// Holds a position inside the ladder. A slider cannot hand over an out-of-range value,
		/// but a scene wired to the wrong maximum can, and a difficulty that silently reads as
		/// something else is worse than one pinned to an end.
		/// </summary>
		public static int Clamp(int i_Stop)
		{
			if (i_Stop < 0)
			{
				return 0;
			}

			return i_Stop >= StopCount ? StopCount - 1 : i_Stop;
		}
	}
}
