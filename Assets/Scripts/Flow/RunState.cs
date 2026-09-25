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
		private bool m_HasSeenInstructions;
		private bool m_HasShownCageRule;
		private bool m_HasExplainedCageStrike;
		private bool m_IsChoosingForReplay;
		private int m_StageNumber;
		private BirdProfile m_SelectedBird;
		private int m_TotalMynasDefeated;
		private int m_TotalPodsPlaced;
		private float m_TotalSeconds;

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

		/// <summary>Mynas defeated across every stage advanced past this run.</summary>
		public int TotalMynasDefeated
		{
			get { return m_TotalMynasDefeated; }
		}

		/// <summary>Pods placed across every stage advanced past this run.</summary>
		public int TotalPodsPlaced
		{
			get { return m_TotalPodsPlaced; }
		}

		/// <summary>Seconds played across every stage advanced past this run.</summary>
		public float TotalSeconds
		{
			get { return m_TotalSeconds; }
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

		/// <summary>
		/// Whether the roster holds a choice worth stopping for. One bird is not a choice, so
		/// the selection screen would only be showing the player a decision already made.
		/// </summary>
		public bool HasBirdChoice
		{
			get { return r_Roster.Count > 1; }
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
			m_HasSeenInstructions = false;
			m_HasShownCageRule = false;
			m_HasExplainedCageStrike = false;
			m_IsChoosingForReplay = false;
			m_TotalMynasDefeated = 0;
			m_TotalPodsPlaced = 0;
			m_TotalSeconds = 0f;

			r_Roster.Clear();

			if (r_StartingBird != null)
			{
				r_Roster.Add(r_StartingBird);
			}

			m_SelectedBird = r_StartingBird;
		}

		/// <summary>
		/// Whether the instructions have been put in front of the player during this run.
		///
		/// Once per run rather than once per stage one: the panel is shown on the way into
		/// the intro, and a death there must not show it again. It is the run that forgets,
		/// so a fresh campaign from the menu teaches the rules over.
		///
		/// Deliberately not <see cref="PlayerPrefs"/>. A player who has not touched the game
		/// in a month is owed the rules again, and a lecturer opening a fresh copy on the
		/// demonstration machine must see what a first player sees.
		/// </summary>
		public bool MarkInstructionsSeen()
		{
			if (m_HasSeenInstructions)
			{
				return false;
			}

			m_HasSeenInstructions = true;

			return true;
		}

		/// <summary>
		/// Claims the one showing of the cage rule this run - "the cage opens when every myna
		/// is gone" - at the first stage that holds a cage. Run-scoped for the same reasons as
		/// <see cref="MarkInstructionsSeen"/>: a retry does not repeat it, a new campaign does.
		/// </summary>
		public bool MarkCageRuleShown()
		{
			if (m_HasShownCageRule)
			{
				return false;
			}

			m_HasShownCageRule = true;

			return true;
		}

		/// <summary>
		/// Claims the one label this run that tells a player whose burst just hit the cage that
		/// pods cannot break it. The clank and the shake answer every hit; the words only the first.
		/// </summary>
		public bool MarkCageStrikeExplained()
		{
			if (m_HasExplainedCageStrike)
			{
				return false;
			}

			m_HasExplainedCageStrike = true;

			return true;
		}

		/// <summary>
		/// Notes that the selection screen about to open is choosing for another attempt at this
		/// stage, not for arriving at the next one. The screen is the same either way; only what
		/// follows the choice differs, and a scene load carries nothing across but the run.
		/// </summary>
		public void MarkChoosingForReplay()
		{
			m_IsChoosingForReplay = true;
		}

		/// <summary>
		/// Whether the choice just made was for a replay, forgetting it either way, so the next
		/// visit to the selection screen - after a stage is cleared - arrives as normal.
		/// </summary>
		public bool ClaimReplayAfterChoice()
		{
			bool isReplay = m_IsChoosingForReplay;

			m_IsChoosingForReplay = false;

			return isReplay;
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

		/// <summary>
		/// Refills the lives and changes nothing else.
		///
		/// Deliberately not <see cref="Restart"/>. Losing the last life costs the lives and
		/// only the lives: the stage reached, the birds earned and the campaign totals all
		/// survive, so a player who runs out on stage five is offered that stage again rather
		/// than the whole campaign from the beginning. Six stages have to be completable in
		/// one sitting.
		/// </summary>
		public void RestoreLives()
		{
			m_Lives = r_StartingLives;
		}

		/// <summary>
		/// Folds one finished stage into the campaign totals the closing results show.
		///
		/// Called when a stage is advanced past, never when its results are merely shown, so
		/// replaying a stage cannot count it twice.
		/// </summary>
		public void AddStageTotals(int i_MynasDefeated, int i_PodsPlaced, float i_Seconds)
		{
			m_TotalMynasDefeated += i_MynasDefeated;
			m_TotalPodsPlaced += i_PodsPlaced;
			m_TotalSeconds += i_Seconds;
		}
	}
}
