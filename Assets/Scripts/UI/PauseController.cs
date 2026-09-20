using BomberBird.Flow;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Stops the stage and shows the pause overlay.
	///
	/// This lives in the scene rather than inside <see cref="GameFlow"/> on purpose. It holds
	/// references to the bird's input components, and those die with every scene reload;
	/// a singleton that survives reloads must never hold them.
	/// </summary>
	public class PauseController : MonoBehaviour
	{
		[Header("Overlay")]
		[SerializeField] private GameObject m_Overlay;

		[Tooltip("The rules panel, opened by the overlay's Help button. It draws over this "
			+ "overlay and owns the keyboard while it is up, so Escape is left alone until "
			+ "it closes - otherwise the keystroke that dismisses the rules would also "
			+ "unpause the game behind them.")]
		[SerializeField] private InstructionsPanel m_Instructions;

		[Header("Input to suspend")]
		[SerializeField] private BirdMovement m_Movement;
		[SerializeField] private BirdPodPlacer m_Placer;

		private bool m_IsPaused;

		public bool IsPaused
		{
			get { return m_IsPaused; }
		}

		private void Awake()
		{
			if (m_Overlay == null)
			{
				Debug.LogError(name + ": m_Overlay is not assigned.", this);
				enabled = false;
				return;
			}

			setPaused(false);
		}

		private void OnDisable()
		{
			// Never leave the game frozen, or silent, because this object went away mid-pause.
			if (m_IsPaused)
			{
				Time.timeScale = 1f;

				if (GameFlow.Instance != null)
				{
					GameFlow.Instance.SetMusicPaused(false);
				}
			}
		}

		private void Update()
		{
			// The rules panel draws over this overlay and takes any key to close. Reading
			// Escape here as well would close the rules and unpause in the same frame.
			if (m_Instructions != null && m_Instructions.IsShown)
			{
				return;
			}

			// Input belongs in Update, and Update still runs while the clock is stopped.
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				setPaused(!m_IsPaused);
			}
		}

		/// <summary>Losing focus pauses the game, per Docs/GDD.md section 4.</summary>
		private void OnApplicationFocus(bool i_HasFocus)
		{
			if (!i_HasFocus && !m_IsPaused)
			{
				setPaused(true);
			}
		}

		/// <summary>
		/// Wired to the overlay's Help button. The rules open over the paused overlay rather
		/// than in place of it, so closing them leaves the player back in the menu they asked
		/// from, with nothing to restore.
		/// </summary>
		public void ShowInstructions()
		{
			if (m_Instructions != null)
			{
				m_Instructions.Show();
			}
		}

		/// <summary>Wired to the overlay's Resume button.</summary>
		public void Resume()
		{
			setPaused(false);
		}

		/// <summary>Wired to the overlay's Restart button. Replays the stage, and costs no life.</summary>
		public void RestartStage()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow in the scene, so the stage cannot restart.", this);
				return;
			}

			// GameFlow restores the clock itself, so a restart never loads into a frozen stage.
			GameFlow.Instance.RestartStage();
		}

		/// <summary>Wired to the overlay's Main Menu button. Abandons the run.</summary>
		public void GoToMainMenu()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow in the scene, so the menu cannot be reached.", this);
				return;
			}

			// GameFlow restores the clock, so the menu never loads into a frozen game.
			GameFlow.Instance.GoToMainMenu();
		}

		private void setPaused(bool i_IsPaused)
		{
			m_IsPaused = i_IsPaused;

			Time.timeScale = i_IsPaused ? 0f : 1f;
			m_Overlay.SetActive(i_IsPaused);

			// A stopped clock already halts movement, which runs in FixedUpdate. Placement
			// reads input in Update, which keeps running, so it has to be switched off.
			if (m_Movement != null)
			{
				m_Movement.enabled = !i_IsPaused;
			}

			if (m_Placer != null)
			{
				m_Placer.enabled = !i_IsPaused;
			}

			// The music runs on its own clock, so stopping time leaves it playing over a
			// paused game.
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.SetMusicPaused(i_IsPaused);
			}
		}
	}
}
