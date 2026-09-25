using System;
using System.Collections;
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

		[Header("Screen transitions")]
		[Tooltip("The black this fades through between screens. Left empty, every load is the "
			+ "hard cut it used to be, which is what a Gameplay scene opened on its own gets.")]
		[SerializeField] private ScreenFade m_Fade;

		[Tooltip("Seconds to fade out, and again to fade in, on arriving somewhere new.")]
		[SerializeField] private float m_FadeSeconds = 0.25f;

		[Tooltip("The same, for replaying the stage just lost. Shorter on purpose: a fade the "
			+ "player sees on every death is a tax on the attempt after it.")]
		[SerializeField] private float m_ReplayFadeSeconds = 0.12f;

		[Tooltip("How far the music is allowed to drop at full black. 0 is silence, which is "
			+ "where the track is swapped and therefore where a swap is least audible. Lift it "
			+ "if the dip on a replay - which changes no track - reads as a dropout.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_MusicDuckFloor;

		[Tooltip("Seconds the music takes to leave when a run ends. The jingle laid over it "
			+ "does not fade - it lands at full while the loop underneath is already going.")]
		[SerializeField] private float m_MusicFadeSeconds = 1.6f;

		// Scenes are loaded by name, never by build index: adding a scene renumbers every
		// index, and an index load would silently start sending the player somewhere else.
		public const string k_MainMenuScene = "MainMenu";
		public const string k_BirdSelectScene = "BirdSelect";
		public const string k_StageIntroScene = "StageIntro";
		public const string k_GameplayScene = "Gameplay";
		public const string k_ClosingScene = "Closing";

		private const string k_EasyMynasPref = "BomberBird.EasyMynas";

		private static GameFlow s_Instance;

		private RunState m_Run;
		private bool m_EasyMynas;
		private AudioSource m_Music;
		private float m_MusicVolume = 1f;
		private Coroutine m_MusicFade;
		private bool m_IsTransitioning;

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

		/// <summary>
		/// Whether a screen transition is running, so nothing takes a keystroke behind the
		/// black. Docs/GDD.md section 5 already promises input is disabled during a stage
		/// transition; this is what that sentence is describing.
		/// </summary>
		public bool IsTransitioning
		{
			get { return m_IsTransitioning; }
		}

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

				// A Web build only writes prefs to browser storage on Save, and closing the tab
				// is not a quit, so without it the setting would be lost between visits.
				PlayerPrefs.Save();
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

			if (m_Music != null)
			{
				// Read once and kept, because the duck writes over the live value and would
				// otherwise ratchet the track quieter with every scene change.
				m_MusicVolume = m_Music.volume;
			}

			m_Run = new RunState(m_StartingLives, m_Campaign == null ? null : m_Campaign.StartingBird);
		}

		/// <summary>
		/// Puts a track on the run's own music source, which outlives every scene load.
		///
		/// A scene asks for what it wants to hear through <see cref="BomberBird.UI.SceneMusic"/>
		/// and this decides whether anything changes: asking for the track already playing is
		/// ignored, so returning to the arena after a results card does not restart the loop
		/// half way through.
		///
		/// A track that is a piece rather than a loop, like the closing celebration, asks not to
		/// repeat: it ends where it ends, and whatever the next screen plays follows it.
		/// </summary>
		public void PlayMusic(AudioClip i_Clip, bool i_IsLooping = true)
		{
			if (m_Music == null || i_Clip == null || (m_Music.clip == i_Clip && m_Music.isPlaying))
			{
				return;
			}

			m_Music.clip = i_Clip;
			m_Music.loop = i_IsLooping;
			m_Music.Play();
		}

		/// <summary>
		/// Takes the music out from under a run that has ended.
		///
		/// The stage loop is the wrong thing to hear over a GAME OVER card, but cutting it dead
		/// on the frame the card arrives is worse than either - the silence reads as a fault.
		/// So it leaves, and the jingle over it does not fade: the sting lands at full while the
		/// loop underneath is already going.
		///
		/// Unscaled, because the card that calls this has just set the clock to zero. The source
		/// is stopped at the end rather than left running silent, so Retry starts the stage's
		/// track from its beginning instead of halfway through the bar that lost.
		/// </summary>
		public void FadeOutMusic()
		{
			if (m_Music == null || !m_Music.isPlaying)
			{
				return;
			}

			stopMusicFade();
			m_MusicFade = StartCoroutine(fadeOutMusic());
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
				// A death replays the stage with the same bird - never back to selection,
				// and never back through the habitat screen.
				ReplayStage();
				return;
			}

			if (raiseStageEnded(eStageOutcome.Failed))
			{
				// The screen owns what happens next, and the arena stays standing behind it.
				return;
			}

			// No screen in the scene. Fall back to the old behaviour rather than stranding
			// the player on a dead arena. A restarted run is arriving at stage 1 rather
			// than replaying it, so this one does go through the habitat screen.
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

			// A replay, not an arrival: this is the stage they just lost on, and they have
			// read its habitat card already.
			ReplayStage();
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

		/// <summary>
		/// Whether the instructions still owe the player a showing this run, claiming that
		/// showing if so. Asked by the panel in the arena as the intro stage opens; a death
		/// there gets false and plays straight on.
		/// </summary>
		public bool ClaimInstructionsShowing()
		{
			return m_Run != null && m_Run.MarkInstructionsSeen();
		}

		/// <summary>
		/// Whether the cage rule still owes the player its one showing this run, claiming it if so.
		/// </summary>
		public bool ClaimCageRuleShowing()
		{
			return m_Run != null && m_Run.MarkCageRuleShown();
		}

		/// <summary>
		/// Whether a burst on the cage still owes the player its one explaining label this run,
		/// claiming it if so.
		/// </summary>
		public bool ClaimCageStrikeExplained()
		{
			return m_Run != null && m_Run.MarkCageStrikeExplained();
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
			// The reset waits for full black. Done before the fade it would be seen: the HUD
			// reads the lives and the stage number every frame, and the card on screen is
			// reporting totals this throws away.
			loadScene(k_MainMenuScene, m_FadeSeconds, restartRun);
		}

		/// <summary>
		/// The end of the campaign: the birds rescued, and the note about the real myna.
		///
		/// The run is deliberately left standing rather than restarted here. The closing
		/// screens read from it, and the menu restarts it on the way out.
		/// </summary>
		public void GoToClosing()
		{
			loadScene(k_ClosingScene, m_FadeSeconds);
		}

		/// <summary>The screen between stages, where the player picks the bird to fly.</summary>
		public void GoToBirdSelect()
		{
			loadScene(k_BirdSelectScene, m_FadeSeconds);
		}

		/// <summary>
		/// Arrives at the stage the run is on, by way of its habitat screen. What Play
		/// calls, what the selection screen calls, and what following one stage with the
		/// next calls.
		///
		/// The intro is reached only from Play and every later stage only from
		/// <see cref="CompleteStage"/>, which is what keeps the selection screen out of the
		/// intro without a rule saying so.
		///
		/// Arriving and replaying are separate methods rather than one with a flag, because
		/// the habitat card belongs to the first and not to the second and every caller
		/// already knows which it means. A player on their fifth attempt at the upland does
		/// not want to be told what an upland is: see <see cref="ReplayStage"/>.
		/// </summary>
		public void StartStage()
		{
			loadScene(k_StageIntroScene, m_FadeSeconds);
		}

		/// <summary>
		/// Leaves the habitat screen and plays the stage it introduced. Called by nothing
		/// else: every other route into the arena either arrives through
		/// <see cref="StartStage"/> or is a replay.
		/// </summary>
		public void BeginStage()
		{
			loadArena(m_FadeSeconds);
		}

		/// <summary>
		/// The same stage over again, with no habitat card in the way. A death, a Retry
		/// after game over, and the pause overlay's Restart all mean this.
		/// </summary>
		public void ReplayStage()
		{
			loadArena(m_ReplayFadeSeconds);
		}

		/// <summary>Replays the stage without spending a life, for the pause overlay.</summary>
		public void RestartStage()
		{
			ReplayStage();
		}

		private void loadArena(float i_Seconds)
		{
			loadScene(k_GameplayScene, i_Seconds);
		}

		private void restartRun()
		{
			if (m_Run != null)
			{
				m_Run.Restart();
			}
		}

		/// <summary>
		/// Every change of screen in the game. Fades to black, loads, and fades back.
		///
		/// One method rather than a fade at each of the five load sites, because a transition
		/// that is only mostly applied is worse than none: the one screen that still cut would
		/// read as a bug in the four that do not.
		///
		/// <paramref name="i_AtBlack"/> is work that must not be seen - state the screen being
		/// left is still displaying. It runs under full black, immediately before the load.
		/// </summary>
		private void loadScene(string i_Scene, float i_Seconds, Action i_AtBlack = null)
		{
			if (m_IsTransitioning)
			{
				// A second request while one is already running: Retry pressed twice, or a
				// key that reached a button standing behind the black. The first is on its way.
				return;
			}

			if (m_Fade == null)
			{
				// No cover on this object. The hard cut is what the game did before this
				// existed, so a GameFlow without one is plain rather than broken.
				Time.timeScale = 1f;

				if (i_AtBlack != null)
				{
					i_AtBlack();
				}

				SceneManager.LoadScene(i_Scene);
				return;
			}

			StartCoroutine(transition(i_Scene, i_Seconds, i_AtBlack));
		}

		private IEnumerator transition(string i_Scene, float i_Seconds, Action i_AtBlack)
		{
			m_IsTransitioning = true;

			// A fade-out still running would write the volume every frame that setCover does,
			// and the two would fight over the same field all the way to black.
			stopMusicFade();

			yield return fade(0f, 1f, i_Seconds);

			// Only now. Held until full black, a Restart from the pause overlay does not run
			// the arena for an eighth of a second behind the cover on its way out.
			Time.timeScale = 1f;

			if (i_AtBlack != null)
			{
				i_AtBlack();
			}

			SceneManager.LoadScene(i_Scene);

			// One frame for the new scene's Awake and Start, so it is dressed before it is
			// uncovered: SceneMusic asks for its track there, and it should arrive under
			// black rather than a beat after the picture.
			yield return null;

			yield return fade(1f, 0f, i_Seconds);

			m_IsTransitioning = false;
		}

		/// <summary>
		/// Walks the cover from one opacity to another on unscaled time, because a transition
		/// out of a paused game runs with the clock stopped.
		/// </summary>
		private void stopMusicFade()
		{
			if (m_MusicFade == null)
			{
				return;
			}

			StopCoroutine(m_MusicFade);
			m_MusicFade = null;

			if (m_Music != null)
			{
				m_Music.volume = m_MusicVolume;
			}
		}

		private IEnumerator fadeOutMusic()
		{
			float elapsed = 0f;

			for (float t = ScreenFade.Ramp(elapsed, m_MusicFadeSeconds); t < 1f;
				t = ScreenFade.Ramp(elapsed, m_MusicFadeSeconds))
			{
				m_Music.volume = m_MusicVolume * (1f - t);

				yield return null;

				elapsed += Time.unscaledDeltaTime;
			}

			m_Music.Stop();

			// Back to the stored level rather than left at zero: the next track to play reads
			// its volume from this source, and one stopped at zero would come back silent.
			m_Music.volume = m_MusicVolume;
			m_MusicFade = null;
		}

		private IEnumerator fade(float i_From, float i_To, float i_Seconds)
		{
			float elapsed = 0f;

			for (float t = ScreenFade.Ramp(elapsed, i_Seconds); t < 1f;
				t = ScreenFade.Ramp(elapsed, i_Seconds))
			{
				setCover(Mathf.Lerp(i_From, i_To, t));

				yield return null;

				elapsed += Time.unscaledDeltaTime;
			}

			setCover(i_To);
		}

		/// <summary>
		/// The cover and the music move together off one number. At full black the music is at
		/// its floor, which is where <see cref="PlayMusic"/> swaps the track and therefore the
		/// quietest place to do it.
		/// </summary>
		private void setCover(float i_Alpha)
		{
			m_Fade.Cover(i_Alpha);

			if (m_Music != null)
			{
				m_Music.volume = m_MusicVolume * Mathf.Lerp(1f, m_MusicDuckFloor, i_Alpha);
			}
		}
	}
}
