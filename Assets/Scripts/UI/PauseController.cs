using BomberBird.Flow;
using BomberBird.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

		[Header("Sound")]
		[Tooltip("The same clip the menus confirm with. Every button on this overlay was silent, "
			+ "which reads as the press not registering when the game behind is frozen anyway.")]
		[SerializeField] private AudioClip m_Confirm;

		[Header("Volume")]
		[Tooltip("Sets SoundLevels.Music. Heard on Resume: the music is held while paused.")]
		[SerializeField] private Slider m_MusicSlider;

		[Tooltip("Sets SoundLevels.Effects.")]
		[SerializeField] private Slider m_EffectsSlider;

		[Tooltip("Played at the new level on each step of the effects slider, so the player hears "
			+ "what they chose without leaving the overlay.")]
		[SerializeField] private AudioClip m_EffectsPreview;

		[Header("Input to suspend")]
		[SerializeField] private BirdMovement m_Movement;
		[SerializeField] private BirdPodPlacer m_Placer;

		private bool m_IsPaused;
		private AudioSource m_PreviewPlaying;

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

			if (m_MusicSlider != null)
			{
				m_MusicSlider.onValueChanged.AddListener(musicSlider_ValueChanged);
			}

			if (m_EffectsSlider != null)
			{
				m_EffectsSlider.onValueChanged.AddListener(effectsSlider_ValueChanged);
			}

			setPaused(false);
		}

		private void OnDisable()
		{
			// Restart and Main Menu leave through here rather than through setPaused.
			SoundLevels.Save();

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
				UiSound.Play(m_Confirm);
				m_Instructions.Show();
			}
		}

		/// <summary>
		/// Wired to the pause button of the on-screen touch pad, which a phone needs in place of
		/// Escape.
		/// </summary>
		public void Pause()
		{
			// The same stand-down as Escape in Update: the rules panel owns the screen while up.
			if (m_Instructions != null && m_Instructions.IsShown)
			{
				return;
			}

			if (!m_IsPaused)
			{
				UiSound.Play(m_Confirm);
				setPaused(true);
			}
		}

		/// <summary>Wired to the overlay's Resume button.</summary>
		public void Resume()
		{
			UiSound.Play(m_Confirm);

			setPaused(false);
		}

		/// <summary>
		/// Wired to the overlay's Restart button. Replays the stage, after a choice of bird when
		/// the roster holds one, and costs no life.
		/// </summary>
		public void RestartStage()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow in the scene, so the stage cannot restart.", this);
				return;
			}

			UiSound.Play(m_Confirm);

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

			UiSound.Play(m_Confirm);

			// GameFlow restores the clock, so the menu never loads into a frozen game.
			GameFlow.Instance.GoToMainMenu();
		}

		private void musicSlider_ValueChanged(float i_Level)
		{
			SoundLevels.Music = i_Level;
		}

		private void effectsSlider_ValueChanged(float i_Level)
		{
			SoundLevels.Effects = i_Level;

			// A drag changes the value every frame. Cut the last preview rather than stack a copy
			// per frame, which is heard as a buzz instead of the level chosen.
			if (m_PreviewPlaying != null)
			{
				m_PreviewPlaying.Stop();
			}

			m_PreviewPlaying = UiSound.Play(m_EffectsPreview);
		}

		private void setPaused(bool i_IsPaused)
		{
			m_IsPaused = i_IsPaused;

			if (i_IsPaused)
			{
				// Hiding the overlay does not clear the selection, so without this it would reopen
				// on whatever was focused last instead of waiting to hand the first key to Resume.
				if (EventSystem.current != null)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}

				// Without notifying: showing the stored level is not the player changing it, and
				// the effects slider would otherwise play its preview as the overlay opens.
				if (m_MusicSlider != null)
				{
					m_MusicSlider.SetValueWithoutNotify(SoundLevels.Music);
				}

				if (m_EffectsSlider != null)
				{
					m_EffectsSlider.SetValueWithoutNotify(SoundLevels.Effects);
				}
			}
			else
			{
				SoundLevels.Save();
			}

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
