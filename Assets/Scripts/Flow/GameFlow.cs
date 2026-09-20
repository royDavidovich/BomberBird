using System;
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
		public const string k_ClosingScene = "Closing";

		private const string k_EasyMynasPref = "BomberBird.EasyMynas";

		private static GameFlow s_Instance;

		private RunState m_Run;
		private bool m_EasyMynas;
		private AudioSource m_Music;

		/// <summary>
		/// Raised when a stage ends, before anything is loaded, so a results screen can show
		/// what just happened while the arena it happened in is still standing.
		///
		/// An event rather than a reference because the screen lives in BomberBird.UI, which
		/// depends on this assembly and not the other way round. Nothing here knows or cares
		/// whether anyone is listening: with no subscriber both paths fall through to what
		/// they did before, so a Gameplay scene opened on its own is still playable.
		/// </summary>
		public event Action<eStageOutcome> StageEnded;

		/// <summary>The live run, or null when no GameFlow is in the scene.</summary>
		public static GameFlow Instance
		{
			get { return s_Instance; }
		}

		public int Lives
		{
			get { return m_Run == null ? 0 : m_Run.Lives; }
		}

		/// <summary>
		/// Whether the mynas walk the easy way: slower, and mostly holding their line rather
		/// than choosing afresh at every junction.
		///
		/// Kept here because it outlives a scene the way the life count does, and remembered
		/// between sittings: a player who needed the easier game last night should not have
		/// to find the setting again. <see cref="StageSetup"/> hands it to the arena.
		/// </summary>
		public bool EasyMynas
		{
			get { return m_EasyMynas; }

			set
			{
				m_EasyMynas = value;
				PlayerPrefs.SetInt(k_EasyMynasPref, value ? 1 : 0);
			}
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

		/// <summary>Whether the stage being played is the campaign's last.</summary>
		public bool IsFinalStage
		{
			get { return m_Campaign != null && StageNumber >= m_Campaign.StageCount; }
		}

		/// <summary>Mynas defeated across every stage advanced past this run.</summary>
		public int TotalMynasDefeated
		{
			get { return m_Run == null ? 0 : m_Run.TotalMynasDefeated; }
		}

		/// <summary>Pods placed across every stage advanced past this run.</summary>
		public int TotalPodsPlaced
		{
			get { return m_Run == null ? 0 : m_Run.TotalPodsPlaced; }
		}

		/// <summary>Seconds played across every stage advanced past this run.</summary>
		public float TotalSeconds
		{
			get { return m_Run == null ? 0f : m_Run.TotalSeconds; }
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

			m_EasyMynas = PlayerPrefs.GetInt(k_EasyMynasPref, 0) != 0;
			m_Music = GetComponent<AudioSource>();

			m_Run = new RunState(m_StartingLives, m_Campaign == null ? null : m_Campaign.StartingBird);
		}

		/// <summary>
		/// Puts a track on the run's own music source, which outlives every scene load.
		///
		/// A scene asks for what it wants to hear through <see cref="BomberBird.UI.SceneMusic"/>
		/// and this decides whether anything changes: asking for the track already playing is
		/// ignored, so returning to the arena after a results card does not restart the loop
		/// half way through.
		/// </summary>
		public void PlayMusic(AudioClip i_Clip)
		{
			if (m_Music == null || i_Clip == null || (m_Music.clip == i_Clip && m_Music.isPlaying))
			{
				return;
			}

			m_Music.clip = i_Clip;
			m_Music.Play();
		}

		/// <summary>
		/// Holds the music where it is, for the pause overlay.
		///
		/// The clock stopping does not quiet it: the source runs on its own time, so a paused
		/// game used to sit under a loop that carried on as though nothing had happened. It
		/// pauses rather than stops, so resuming picks the bar back up instead of restarting
		/// the track.
		/// </summary>
		public void SetMusicPaused(bool i_IsPaused)
		{
			if (m_Music == null)
			{
				return;
			}

			if (i_IsPaused)
			{
				m_Music.Pause();
			}
			else
			{
				m_Music.UnPause();
			}
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
		/// Spends a life. An ordinary death replays the stage; the last one ends the run and
		/// hands the decision to the player.
		///
		/// Losing the last life used to call <see cref="RunState.Restart"/>, which wiped the
		/// stage reached and every bird earned and dropped the player into stage one with no
		/// screen and no explanation. It now stops here: the results screen reports it, and
		/// the player chooses between another attempt at this same stage, with the roster
		/// intact, and the menu. Six stages have to be completable in one sitting.
		/// </summary>
		public void ReportDeath()
		{
			if (!m_Run.LoseLife())
			{
				// A death replays the stage with the same bird - never back to selection.
				StartStage();
				return;
			}

			if (raiseStageEnded(eStageOutcome.Failed))
			{
				// The screen owns what happens next, and the arena stays standing behind it.
				return;
			}

			// No screen in the scene. Fall back to the old behaviour rather than stranding
			// the player on a dead arena.
			m_Run.Restart();
			StartStage();
		}

		/// <summary>
		/// The stage was cleared. Reports it, and carries the run forward once the player has
		/// seen the result.
		///
		/// The event is raised before the run advances, so the screen still reads the stage
		/// that was just played rather than the one coming next.
		/// </summary>
		public void CompleteStage()
		{
			if (raiseStageEnded(eStageOutcome.Cleared))
			{
				// The screen calls AdvanceToNextStage when the player is done reading.
				return;
			}

			AdvanceToNextStage(0, 0, 0f);
		}

		/// <summary>
		/// Leaves the stage just cleared and plays whatever comes after it, folding that
		/// stage's tally into the campaign totals on the way.
		///
		/// Split out of <see cref="CompleteStage"/> so the results screen has somewhere to go
		/// that does not raise the event it is already answering.
		/// </summary>
		public void AdvanceToNextStage(int i_MynasDefeated, int i_PodsPlaced, float i_Seconds)
		{
			m_Run.AddStageTotals(i_MynasDefeated, i_PodsPlaced, i_Seconds);
			m_Run.AdvanceStage();

			// Past the last stage the campaign is over. Until now this fell through to
			// StartStage, which loaded a Gameplay scene the campaign had no stage for, and
			// StageSetup quietly used the scene's own default arena instead.
			if (m_Campaign != null && m_Run.StageNumber > m_Campaign.StageCount)
			{
				GoToClosing();
				return;
			}

			// Every stage after the intro is chosen into, but only once there is something to
			// choose. A roster of one would show the player a decision already made.
			if (m_Run.HasBirdChoice)
			{
				GoToBirdSelect();
			}
			else
			{
				StartStage();
			}
		}

		/// <summary>
		/// Another attempt at this stage after the last life was spent. The lives come back;
		/// the stage reached, the birds earned and the campaign totals do not move.
		/// </summary>
		public void RetryAfterGameOver()
		{
			m_Run.RestoreLives();

			StartStage();
		}

		/// <summary>Reports the outcome, and says whether anyone was listening.</summary>
		private bool raiseStageEnded(eStageOutcome i_Outcome)
		{
			Action<eStageOutcome> ended = StageEnded;

			if (ended == null)
			{
				return false;
			}

			ended(i_Outcome);

			return true;
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

		/// <summary>
		/// The end of the campaign: the birds rescued, and the note about the real myna.
		///
		/// The run is deliberately left standing rather than restarted here. The closing
		/// screens read from it, and the menu restarts it on the way out.
		/// </summary>
		public void GoToClosing()
		{
			Time.timeScale = 1f;

			SceneManager.LoadScene(k_ClosingScene);
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
