using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The end of the campaign, in three panels on one scene: the valley handed back with feathers
	/// thrown in from both sides, the birds the player rescued, and then the note about the
	/// common myna as a real invasive species in Israel.
	///
	/// One scene rather than three, because each panel is a page turn rather than a place.
	/// A second scene would want its own canvas, its own entry in the build settings and its own
	/// copy of everything already loaded here, to show a card and take one keypress.
	///
	/// Neither panel decides anything. Removing this component would leave the campaign fully
	/// playable and only cost the player the ending, which is the separation Docs/GDD.md section 7
	/// asks of presentation.
	/// </summary>
	public class ClosingScreen : MonoBehaviour
	{
		/// <summary>
		/// One card on the rescued panel and the bird it stands for. Paired rather than left as
		/// two parallel arrays, because the pairing is the whole point and a list that slipped
		/// by one would credit the player with the wrong bird.
		/// </summary>
		[System.Serializable]
		private class RescuedCard
		{
			[Tooltip("The bird the card names. The run's roster is searched for it.")]
			public BirdProfile Bird;

			[Tooltip("The card itself, hidden when the bird was never rescued.")]
			public RectTransform Card;
		}

		[Header("Panels, in the order they are shown")]
		[Tooltip("\"The valley is yours again\", with the feathers. The celebration before the "
			+ "birds are named.")]
		[SerializeField] private GameObject m_ValleyPanel;

		[Tooltip("The birds rescued across the campaign.")]
		[SerializeField] private GameObject m_RescuedPanel;

		[Tooltip("The factual card about the myna.")]
		[SerializeField] private GameObject m_NotePanel;

		[Header("The birds rescued")]
		[Tooltip("Every card the panel can show. A bird the run never earned is hidden and "
			+ "the cards left standing close the gap.")]
		[SerializeField] private RescuedCard[] m_RescuedCards;

		[Tooltip("Gap between the cards left standing. Matches the bird selection row.")]
		[SerializeField] private float m_CardSpacing = 36f;

		[Header("Prompt")]
		[Tooltip("Hint that a press moves on. Hidden until the player has had a moment to read.")]
		[SerializeField] private GameObject m_ContinueHint;

		[Tooltip("Seconds the valley holds before a press is accepted. Long enough for the burst "
			+ "to land, and for a key still held from the boss not to skip the celebration.")]
		[SerializeField] private float m_ValleyHold = 3f;

		[Tooltip("Seconds the rescued birds hold before a press is accepted.")]
		[SerializeField] private float m_RescuedHold = 3f;

		[Tooltip("Seconds the note holds before a press is accepted. The longest hold in the "
			+ "game: this card is the reason it exists, and a press meant for the screen before "
			+ "it would throw it away unread.")]
		[SerializeField] private float m_NoteHold = 8f;

		[Tooltip("Seconds for one full fade of the prompt once it appears. It pulses rather "
			+ "than blinks: after an eight second wait a hard on and off becomes irritating, "
			+ "and the point is only to say the screen is listening now.")]
		[SerializeField] private float m_PulseSeconds = 1.4f;

		[Tooltip("How far down the pulse fades. 1 would not move at all.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_PulseFloor = 0.3f;

		[Header("Music")]
		[Tooltip("The celebration under the valley panel. Played once, not looped.")]
		[SerializeField] private AudioClip m_ValleyMusic;

		[Tooltip("The ending track, from the rescued birds to the end.")]
		[SerializeField] private AudioClip m_EndingMusic;

		private float m_ShownAt;
		private float m_Hold;
		private int m_Page;
		private TMP_Text m_HintText;

		private void Start()
		{
			ShowRescued(GameFlow.Instance == null ? null : GameFlow.Instance.Roster);
			showPage(0);
		}

		/// <summary>
		/// Leaves standing only the birds the run actually rescued, and closes the row up around
		/// the gaps. Docs/GDD.md section 5.7 lists the birds the player saved, and the feathers
		/// are optional - a card for a bird that was never freed is the screen claiming something
		/// that did not happen.
		///
		/// A null roster shows every card. That is the scene opened on its own in the editor,
		/// where the whole panel is what you want to see, and it is also why this takes the
		/// roster rather than reading the singleton itself: the test can hand it one.
		/// </summary>
		public void ShowRescued(IList<BirdProfile> i_Rescued)
		{
			if (m_RescuedCards == null)
			{
				return;
			}

			List<RectTransform> standing = new List<RectTransform>();

			for (int i = 0; i < m_RescuedCards.Length; ++i)
			{
				RescuedCard card = m_RescuedCards[i];

				if (card == null || card.Card == null)
				{
					continue;
				}

				bool wasRescued = i_Rescued == null
					|| (card.Bird != null && i_Rescued.Contains(card.Bird));

				card.Card.gameObject.SetActive(wasRescued);

				if (wasRescued)
				{
					standing.Add(card.Card);
				}
			}

			if (standing.Count == 0)
			{
				return;
			}

			// The cards are the same width, so one step serves all of them. Their authored
			// height is left alone: only the row needs recentring.
			float step = standing[0].rect.width + m_CardSpacing;
			float firstX = -step * (standing.Count - 1) * 0.5f;

			for (int i = 0; i < standing.Count; ++i)
			{
				RectTransform card = standing[i];

				card.anchoredPosition = new Vector2(firstX + step * i, card.anchoredPosition.y);
			}
		}

		private void Update()
		{
			if (Time.unscaledTime - m_ShownAt < m_Hold)
			{
				return;
			}

			if (m_ContinueHint != null && !m_ContinueHint.activeSelf)
			{
				m_ContinueHint.SetActive(true);
			}

			pulseHint();

			if (!wasPressed())
			{
				return;
			}

			if (m_Page == 2)
			{
				// The menu restarts the run, which is what makes the next Play a fresh campaign
				// rather than one that thinks it has already finished.
				if (GameFlow.Instance != null)
				{
					GameFlow.Instance.GoToMainMenu();
				}

				return;
			}

			int next = m_Page + 1;

			// Through black, the same as every change of screen: the beats are screens in
			// their own right, and a hard cut between two of them read as a glitch.
			if (GameFlow.Instance != null)
			{
				// The music is kept through every turn but the first: only leaving the valley
				// changes the track, and that swap wants the quiet of the duck to hide in.
				GameFlow.Instance.FadeThrough(() => showPage(next), m_Page != 0);
			}
			else
			{
				showPage(next);
			}
		}

		/// <summary>
		/// Turns to one of the three panels: 0 the valley, 1 the rescued birds, 2 the note.
		/// </summary>
		private void showPage(int i_Page)
		{
			m_Page = i_Page;

			setShown(m_ValleyPanel, i_Page == 0);
			setShown(m_RescuedPanel, i_Page == 1);
			setShown(m_NotePanel, i_Page == 2);

			// The celebration belongs to the valley alone, and plays once: a fanfare that
			// starts over reads as a glitch. The ending track starts with the birds and loops
			// on through the note, where PlayMusic ignores the repeat.
			if (GameFlow.Instance != null)
			{
				if (i_Page == 0)
				{
					GameFlow.Instance.PlayMusic(m_ValleyMusic, false);
				}
				else
				{
					GameFlow.Instance.PlayMusic(m_EndingMusic);
				}
			}

			restartHint(i_Page == 0 ? m_ValleyHold : i_Page == 1 ? m_RescuedHold : m_NoteHold);
		}

		/// <summary>
		/// Breathes the prompt in and out. Unscaled, so it keeps moving whatever the game has
		/// done to Time.timeScale on its way here.
		/// </summary>
		private void pulseHint()
		{
			if (m_ContinueHint == null)
			{
				return;
			}

			if (m_HintText == null)
			{
				m_HintText = m_ContinueHint.GetComponent<TMP_Text>();

				if (m_HintText == null)
				{
					return;
				}
			}

			m_HintText.alpha = UiPulse.Alpha(
				Time.unscaledTime - m_ShownAt - m_Hold, m_PulseSeconds, m_PulseFloor);
		}

		private static void setShown(GameObject i_Panel, bool i_IsShown)
		{
			if (i_Panel != null)
			{
				i_Panel.SetActive(i_IsShown);
			}
		}

		private void restartHint(float i_Hold)
		{
			if (m_ContinueHint != null)
			{
				m_ContinueHint.SetActive(false);

				if (m_HintText != null)
				{
					m_HintText.alpha = 1f;
				}
			}

			m_ShownAt = Time.unscaledTime;
			m_Hold = i_Hold;
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
