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
	/// that component's job is the pause overlay and this must not show one.
	/// </summary>
	public class StageReady : MonoBehaviour
	{
		[Header("Parts")]
		[Tooltip("Shown while the stage waits. Hidden for good once it starts.")]
		[SerializeField] private GameObject m_Prompt;

		[Header("What it suspends")]
		[Tooltip("Switched off while waiting, so Escape cannot pause a stage that has not "
			+ "started.")]
		[SerializeField] private PauseController m_Pause;

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

			pulsePrompt();

			if (Input.anyKeyDown)
			{
				m_IsReleasing = true;
			}
		}

		private void hold()
		{
			m_IsWaiting = true;

			if (m_Pause != null)
			{
				m_Pause.enabled = false;
			}

			Time.timeScale = 0f;

			if (m_Movement != null)
			{
				m_Movement.enabled = false;
			}

			if (m_Placer != null)
			{
				m_Placer.enabled = false;
			}

			if (m_Prompt != null)
			{
				m_Prompt.SetActive(true);
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

			if (m_Pause != null)
			{
				m_Pause.enabled = true;
			}

			if (m_Prompt != null)
			{
				m_Prompt.SetActive(false);
			}

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
