using System;
using System.Collections.Generic;
using BomberBird.Player;

namespace BomberBird.Flow
{
	/// <summary>
	/// One run through the campaign: the lives still in hand, the stage being played, and
	/// the birds earned along the way.
	///
	/// The roster lives here rather than in a save file because it belongs to the run. The
	/// Unity reference rules PlayerPrefs out for authoritative progress, and a roster that
	/// survived a finished run would make the feather unlock meaningless on the replay.
	///
	/// Plain C# with no scene and no MonoBehaviour, so spending a last life can be stepped
	/// through in a test instead of by dying three times in Play Mode.
	/// </summary>
	public class RunState
	{
		public const int k_FirstStage = 1;

		private readonly int r_StartingLives;
		private readonly BirdProfile r_StartingBird;
		private readonly List<BirdProfile> r_Roster = new List<BirdProfile>();

		private int m_Lives;
		private int m_StageNumber;
		private BirdProfile m_SelectedBird;

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

		/// <summary>Every bird the player may choose from, starter first, in unlock order.</summary>
		public IList<BirdProfile> Roster
		{
			get { return r_Roster; }
		}

		/// <summary>The bird being played.</summary>
		public BirdProfile SelectedBird
		{
			get { return m_SelectedBird; }
		}

		/// <summary>
		/// A run always starts with at least one life. A zero or negative setting is a
		/// misconfigured Inspector value rather than a reason to refuse to play, so it is
		/// raised to one instead of throwing.
		/// </summary>
		public RunState(int i_StartingLives, BirdProfile i_StartingBird)
		{
			r_StartingLives = Math.Max(1, i_StartingLives);
			r_StartingBird = i_StartingBird;

			Restart();
		}

		/// <summary>Back to full lives, at the first stage, with only the starter bird.</summary>
		public void Restart()
		{
			m_Lives = r_StartingLives;
			m_StageNumber = k_FirstStage;

			r_Roster.Clear();

			if (r_StartingBird != null)
			{
				r_Roster.Add(r_StartingBird);
			}

			m_SelectedBird = r_StartingBird;
		}

		/// <summary>
		/// Adds a bird to the roster. Returns whether it was actually new, so a caller can
		/// tell a first unlock from a feather collected on a replayed stage.
		/// </summary>
		public bool UnlockBird(BirdProfile i_Bird)
		{
			if (i_Bird == null || r_Roster.Contains(i_Bird))
			{
				return false;
			}

			r_Roster.Add(i_Bird);

			return true;
		}

		/// <summary>
		/// Chooses the bird to play. Refuses one that has not been earned, so a selection
		/// screen bug cannot hand the player a bird the campaign has not given them.
		/// </summary>
		public bool SelectBird(BirdProfile i_Bird)
		{
			if (i_Bird == null || !r_Roster.Contains(i_Bird))
			{
				return false;
			}

			m_SelectedBird = i_Bird;

			return true;
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
