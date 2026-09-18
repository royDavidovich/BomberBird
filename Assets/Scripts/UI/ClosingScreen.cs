using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The end of the campaign, in two panels on one scene: the birds the player rescued, and
	/// then the note about the common myna as a real invasive species in Israel.
	///
	/// One scene rather than two, because the second panel is a page turn rather than a place.
	/// A second scene would want its own canvas, its own entry in the build settings and its own
	/// copy of everything already loaded here, to show a card and take one keypress.
	///
	/// Neither panel decides anything. Removing this component would leave the campaign fully
	/// playable and only cost the player the ending, which is the separation Docs/GDD.md section 7
	/// asks of presentation.
	/// </summary>
	public class ClosingScreen : MonoBehaviour
	{
		[Header("Panels, in the order they are shown")]
		[Tooltip("The birds rescued across the campaign.")]
		[SerializeField] private GameObject m_RescuedPanel;

		[Tooltip("The factual card about the myna.")]
		[SerializeField] private GameObject m_NotePanel;

		[Header("Prompt")]
		[Tooltip("Hint that a press moves on. Hidden until the player has had a moment to read.")]
		[SerializeField] private GameObject m_ContinueHint;

		[Tooltip("Seconds before a press is accepted, so the last keystroke of the boss fight "
			+ "does not skip the ending that keystroke just earned.")]
		[SerializeField] private float m_HoldBeforeInput = 0.75f;

		private float m_ShownAt;
		private bool m_IsOnNote;

		private void Start()
		{
			show(m_RescuedPanel, m_NotePanel);
		}

		private void Update()
		{
			if (Time.unscaledTime - m_ShownAt < m_HoldBeforeInput)
			{
				return;
			}

			if (m_ContinueHint != null && !m_ContinueHint.activeSelf)
			{
				m_ContinueHint.SetActive(true);
			}

			if (!wasPressed())
			{
				return;
			}

			if (m_IsOnNote)
			{
				// The menu restarts the run, which is what makes the next Play a fresh campaign
				// rather than one that thinks it has already finished.
				if (GameFlow.Instance != null)
				{
					GameFlow.Instance.GoToMainMenu();
				}

				return;
			}

			m_IsOnNote = true;
			show(m_NotePanel, m_RescuedPanel);
		}

		private void show(GameObject i_Shown, GameObject i_Hidden)
		{
			if (i_Hidden != null)
			{
				i_Hidden.SetActive(false);
			}

			if (i_Shown != null)
			{
				i_Shown.SetActive(true);
			}

			if (m_ContinueHint != null)
			{
				m_ContinueHint.SetActive(false);
			}

			m_ShownAt = Time.unscaledTime;
		}

		/// <summary>
		/// Any key or any mouse button. There is nothing to choose on either panel, so asking
		/// the player to find a particular one would be a puzzle rather than a prompt.
		/// </summary>
		private static bool wasPressed()
		{
			return Input.anyKeyDown;
		}
	}
}
