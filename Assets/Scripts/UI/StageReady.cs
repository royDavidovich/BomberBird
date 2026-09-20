using BomberBird.Player;
using TMPro;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Holds the stage still until the player presses something.
	///
	/// Every load of the arena arrives with the player's hands somewhere else: a new stage
	/// follows a screen they were reading, and a retry follows the death or the results card
	/// that sent them back. Starting live means the first myna is already moving before the
	/// player has looked at the board. This gives them the beat to look.
	///
	/// It is not part of <see cref="PauseController"/>, which uses the same freeze, because
	/// that component's job is the pause overlay and this must not show one. The two do run
	/// side by side, though: pausing a stage that has not begun is reasonable - the player
	/// wants the menu, not the arena - so the hold survives a pause and the resume that ends
	/// it, and only Space lets the stage go.
	///
	/// The prompt sits under both overlays in the canvas, so a pause draws over it rather
	/// than the other way round. Where it sits today that makes no visible difference - the
	/// pause panel covers the prompt's whole line - but it is the order that lets the prompt
	/// move anywhere outside the panel without reaching over the menu.
	/// </summary>
	public class StageReady : MonoBehaviour
	{
		[Header("Parts")]
		[Tooltip("Shown while the stage waits. Hidden for good once it starts.")]
		[SerializeField] private GameObject m_Prompt;

		[Header("What it suspends")]
		[Tooltip("Left running, so Escape still opens the menu on a stage that has not begun. "
			+ "Read here to know when the hold is the pause's rather than this one's.")]
		[SerializeField] private PauseController m_Pause;

		[Tooltip("The rules panel, shown over the held stage on the way into the intro. It "
			+ "takes any key to close, so the stage must not also take that key and start.")]
		[SerializeField] private InstructionsPanel m_Instructions;

		[Tooltip("Stopped by hand: a frozen clock halts movement, but pod placing reads input "
			+ "in Update and would keep working.")]
		[SerializeField] private BirdMovement m_Movement;
		[SerializeField] private BirdPodPlacer m_Placer;

		[Header("Prompt")]
		[Tooltip("Seconds for one full fade of the prompt, matching the closing screens.")]
		[SerializeField] private float m_PulseSeconds = 1.4f;

		[Range(0f, 1f)]
		[SerializeField] private float m_PulseFloor = 0.3f;

		private TMP_Text m_PromptText;
		private bool m_IsWaiting;
		private bool m_IsReleasing;

		private void Start()
		{
			hold();
		}

		private void Update()
		{
			if (m_IsReleasing)
			{
				// One frame later than the press. Releasing in the same frame would hand the
				// very keystroke that started the stage to BirdPodPlacer, which reads input in
				// Update, and the player would drop a pod they never asked for.
				release();
				return;
			}

			if (!m_IsWaiting)
			{
				return;
			}

			if (m_Instructions != null && m_Instructions.IsShown)
			{
				// The rules are up over the held arena. They close on any key, and Space is
				// a key, so a player dismissing them would otherwise start the stage in the
				// same keystroke - which is exactly the beat this hold exists to give them.
				// The clock is still held below, so nothing moves behind the panel.
				holdClock();
				return;
			}

			if (m_Pause != null && m_Pause.IsPaused)
			{
				// The pause overlay is up over the held stage. It owns the screen until it
				// closes, and Resume hands the clock back to this hold rather than to play.
				//
				// The prompt is left on screen and simply falls behind the menu, because it
				// now sits under the overlay in the canvas rather than over it. Nothing else
				// in this method runs while the menu is up: the pulse stops, so the prompt
				// holds whatever alpha it had, and the Space check below is skipped, or the
				// keystroke that presses Resume would also start the stage behind it.
				return;
			}

			showPrompt(true);
			holdClock();
			pulsePrompt();

			// Space alone, and never as part of a system shortcut. This used to take any key,
			// which meant Escape started the stage on its way to opening the pause overlay,
			// and a Cmd+Shift+4 screenshot started it by accident.
			if (Input.GetKeyDown(KeyCode.Space) && !UiInput.IsModifierHeld())
			{
				m_IsReleasing = true;
			}
		}

		private void hold()
		{
			m_IsWaiting = true;

			holdClock();
			showPrompt(true);
		}

		private void showPrompt(bool i_IsShown)
		{
			if (m_Prompt != null && m_Prompt.activeSelf != i_IsShown)
			{
				m_Prompt.SetActive(i_IsShown);
			}
		}

		/// <summary>
		/// Re-asserted every waiting frame rather than set once, because the pause overlay
		/// can open over a held stage and its Resume restores the clock and the bird's input
		/// on the way out. Without this the stage would be live behind a prompt still asking
		/// the player to start it.
		/// </summary>
		private void holdClock()
		{
			Time.timeScale = 0f;

			if (m_Movement != null)
			{
				m_Movement.enabled = false;
			}

			if (m_Placer != null)
			{
				m_Placer.enabled = false;
			}
		}

		private void release()
		{
			m_IsWaiting = false;
			m_IsReleasing = false;

			Time.timeScale = 1f;

			if (m_Movement != null)
			{
				m_Movement.enabled = true;
			}

			if (m_Placer != null)
			{
				m_Placer.enabled = true;
			}

			showPrompt(false);

			// Nothing left to do for the rest of the stage.
			enabled = false;
		}

		/// <summary>
		/// Breathes the prompt, the way the closing screens do. Unscaled, because the clock it
		/// would otherwise read is the one being held at zero.
		/// </summary>
		private void pulsePrompt()
		{
			if (m_Prompt == null)
			{
				return;
			}

			if (m_PromptText == null)
			{
				m_PromptText = m_Prompt.GetComponent<TMP_Text>();

				if (m_PromptText == null)
				{
					return;
				}
			}

			if (m_PulseSeconds <= 0f)
			{
				m_PromptText.alpha = 1f;
				return;
			}

			float wave = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime / m_PulseSeconds * 2f * Mathf.PI);

			m_PromptText.alpha = Mathf.Lerp(m_PulseFloor, 1f, wave);
		}
	}
}
