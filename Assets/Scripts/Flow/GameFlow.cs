using System.Collections.Generic;
using BomberBird.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BomberBird.Flow
{
	/// <summary>
	/// What starts and ends a stage: the lives in hand, the stage being played, and the
	/// scene loads that follow a death or a completed stage.
	///
	/// This is the only singleton in BomberBird, and it earns that narrowly. A retry reloads
	/// the gameplay scene, so the life count has to outlive the reload - state that genuinely
	/// spans scenes is the one case CONVENTIONS.md allows. It is deliberately kept to that
	/// job: a singleton that accumulates unrelated work is a service locator in disguise.
	/// </summary>
	public class GameFlow : MonoBehaviour
	{
		[Tooltip("Attempts before the run ends and starts over.")]
		[SerializeField] private int m_StartingLives = 3;

		[Tooltip("The campaign this run plays: the stages in order and the birds they award.")]
		[SerializeField] private Campaign m_Campaign;

		// Scenes are loaded by name, never by build index: adding a scene renumbers every
		// index, and an index load would silently start sending the player somewhere else.
		public const string k_MainMenuScene = "MainMenu";
		public const string k_BirdSelectScene = "BirdSelect";
		public const string k_GameplayScene = "Gameplay";

		private static GameFlow s_Instance;

		private RunState m_Run;

		/// <summary>The live run, or null when no GameFlow is in the scene.</summary>
		public static GameFlow Instance
		{
			get { return s_Instance; }
		}

		public int Lives
		{
			get { return m_Run == null ? 0 : m_Run.Lives; }
		}

		public int StageNumber
		{
			get { return m_Run == null ? RunState.k_FirstStage : m_Run.StageNumber; }
		}

		/// <summary>Every bird earned so far, for the selection screen.</summary>
		public IList<BirdProfile> Roster
		{
			get { return m_Run == null ? null : m_Run.Roster; }
		}

		/// <summary>The bird being played, whose handling the stage is set up with.</summary>
		public BirdProfile SelectedBird
		{
			get { return m_Run == null ? null : m_Run.SelectedBird; }
		}

		/// <summary>The stage being played, or null once the campaign has been finished.</summary>
		public Campaign.Stage CurrentStage
		{
			get { return m_Campaign == null ? null : m_Campaign.GetStage(StageNumber); }
		}

		/// <summary>
		/// The bird this stage's feather unlocks, or null when it awards none. Already-earned
		/// birds count as none, so replaying a stage does not drop a feather for a bird the
		/// player is already carrying.
		/// </summary>
		public BirdProfile AwardedBird
		{
			get
			{
				Campaign.Stage stage = CurrentStage;

				if (stage == null || stage.AwardedBird == null || m_Run == null)
				{
					return null;
				}

				return m_Run.Roster.Contains(stage.AwardedBird) ? null : stage.AwardedBird;
			}
		}

		private void Awake()
		{
			if (s_Instance != null && s_Instance != this)
			{
				// Returning to a scene that also carries one must not stack a second run.
				Destroy(gameObject);
				return;
			}

			s_Instance = this;
			DontDestroyOnLoad(gameObject);

			if (m_Campaign != null)
			{
				string problems = m_Campaign.DescribeProblems();

				if (problems != null)
				{
					Debug.LogError(name + ": campaign '" + m_Campaign.name + "' is unusable:" + problems, this);
				}
			}

			m_Run = new RunState(m_StartingLives, m_Campaign == null ? null : m_Campaign.StartingBird);
		}

		private void OnDestroy()
		{
			if (s_Instance == this)
			{
				// Entering Play Mode without a domain reload keeps statics alive, and a stale
				// reference to a destroyed object is worse than none.
				s_Instance = null;
			}
		}

		/// <summary>
		/// Spends a life and replays the stage. Spending the last one starts the run over,
		/// which is the whole failure path until a results screen exists.
		/// </summary>
		public void ReportDeath()
		{
			if (m_Run.LoseLife())
			{
				m_Run.Restart();
			}

			// A death replays the stage with the same bird - never back to selection.
			StartStage();
		}

		/// <summary>The stage was cleared. Carries the run forward and plays the next one.</summary>
		public void CompleteStage()
		{
			m_Run.AdvanceStage();

			// Not straight into the next stage: every stage after the intro is chosen into.
			GoToBirdSelect();
		}

		/// <summary>
		/// Adds a bird to the roster, for a feather the player has just collected. Returns
		/// whether it was new.
		/// </summary>
		public bool UnlockBird(BirdProfile i_Bird)
		{
			return m_Run != null && m_Run.UnlockBird(i_Bird);
		}

		/// <summary>Chooses the bird to play, for the selection screen.</summary>
		public bool SelectBird(BirdProfile i_Bird)
		{
			return m_Run != null && m_Run.SelectBird(i_Bird);
		}

		/// <summary>
		/// Abandons the run and returns to the menu. The roster belongs to the run, so
		/// leaving mid-campaign starts the next one clean rather than half-finished.
		/// </summary>
		public void GoToMainMenu()
		{
			Time.timeScale = 1f;

			if (m_Run != null)
			{
				m_Run.Restart();
			}

			SceneManager.LoadScene(k_MainMenuScene);
		}

		/// <summary>The screen between stages, where the player picks the bird to fly.</summary>
		public void GoToBirdSelect()
		{
			Time.timeScale = 1f;

			SceneManager.LoadScene(k_BirdSelectScene);
		}

		/// <summary>
		/// Plays the stage the run is on. What Play calls, and what Continue calls.
		///
		/// The intro is reached only from Play and every later stage only from
		/// <see cref="CompleteStage"/>, which is what keeps the selection screen out of the
		/// intro without a rule saying so.
		/// </summary>
		public void StartStage()
		{
			Time.timeScale = 1f;

			SceneManager.LoadScene(k_GameplayScene);
		}

		/// <summary>Replays the stage without spending a life, for the pause overlay.</summary>
		public void RestartStage()
		{
			// Same load as starting it: a retry is the stage over again, not a different one.
			StartStage();
		}
	}
}
