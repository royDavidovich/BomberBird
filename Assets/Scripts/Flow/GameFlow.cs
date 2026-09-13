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

			m_Run = new RunState(m_StartingLives);
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

			reloadStage();
		}

		/// <summary>The stage was cleared. Carries the run forward and plays the next one.</summary>
		public void CompleteStage()
		{
			m_Run.AdvanceStage();

			reloadStage();
		}

		/// <summary>Replays the stage without spending a life, for the pause overlay.</summary>
		public void RestartStage()
		{
			reloadStage();
		}

		private void reloadStage()
		{
			// A restart from the pause overlay would otherwise load into a stopped clock.
			Time.timeScale = 1f;

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}
}
