using System;

namespace BomberBird.Flow
{
	/// <summary>
	/// One run through the campaign: the lives still in hand and the stage being played.
	///
	/// Plain C# with no scene and no MonoBehaviour, so spending a last life can be stepped
	/// through in a test instead of by dying three times in Play Mode.
	/// </summary>
	public class RunState
	{
		public const int k_FirstStage = 1;

		private readonly int r_StartingLives;

		private int m_Lives;
		private int m_StageNumber;

		/// <summary>Lives still in hand. Zero means the run is over.</summary>
		public int Lives
		{
			get { return m_Lives; }
		}

		/// <summary>The stage being played, numbered the way the player sees it.</summary>
		public int StageNumber
		{
			get { return m_StageNumber; }
		}

		public bool IsOver
		{
			get { return m_Lives <= 0; }
		}

		/// <summary>
		/// A run always starts with at least one life. A zero or negative setting is a
		/// misconfigured Inspector value rather than a reason to refuse to play, so it is
		/// raised to one instead of throwing.
		/// </summary>
		public RunState(int i_StartingLives)
		{
			r_StartingLives = Math.Max(1, i_StartingLives);

			Restart();
		}

		/// <summary>Back to full lives, at the first stage.</summary>
		public void Restart()
		{
			m_Lives = r_StartingLives;
			m_StageNumber = k_FirstStage;
		}

		/// <summary>
		/// Spends a life. Returns whether the run is now over, so a caller never has to read
		/// <see cref="Lives"/> to find out what to do next.
		/// </summary>
		public bool LoseLife()
		{
			if (m_Lives > 0)
			{
				--m_Lives;
			}

			return IsOver;
		}

		/// <summary>Moves to the next stage. Lives carry over.</summary>
		public void AdvanceStage()
		{
			++m_StageNumber;
		}
	}
}
