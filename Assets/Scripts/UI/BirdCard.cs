using System;
using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// One bird on the selection screen: a field-guide plate carrying its portrait, its name,
	/// and the three values that make it different.
	///
	/// A bird not yet earned is drawn as a flat silhouette of its own portrait rather than a
	/// placeholder, so it can never fall out of step with the bird it stands for, and it shows
	/// only the habitat that awards it. The reveal is the reward.
	///
	/// A locked card is still reachable. The player can walk the cursor onto a bird they have
	/// not earned and read its habitat, which is the clue that makes it worth walking towards;
	/// what it will not do is let them fly it.
	///
	/// Presentation only. It reports that it was pressed and knows nothing about whether the
	/// run will accept that.
	/// </summary>
	public class BirdCard : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		[Header("Parts")]
		[SerializeField] private Image m_Portrait;
		[SerializeField] private TMP_Text m_NameLabel;
		[Tooltip("The three values, one per row, with the number on the right. Stacked rather "
			+ "than run together on one line so two birds can be compared by reading straight "
			+ "across the row.")]
		[SerializeField] private TMP_Text m_SpeedValue;
		[SerializeField] private TMP_Text m_BurstValue;
		[SerializeField] private TMP_Text m_PodsValue;
		[SerializeField] private TMP_Text m_HabitatLabel;
		[SerializeField] private Button m_Button;

		[Tooltip("The corner bracket, shown while this card has the player's attention.")]
		[SerializeField] private GameObject m_FocusBracket;

		[Tooltip("Dark wash over a bird not yet earned. It sits under the bracket, so a locked "
			+ "card that is focused still shows its focus at full strength.")]
		[SerializeField] private GameObject m_LockedScrim;

		[Header("Focus")]
		[Tooltip("How far the card lifts while focused. Nothing else in the row moves, because "
			+ "the lift is a position offset rather than a change of size.")]
		[SerializeField] private float m_FocusLift = 16f;

		[Header("Locked look")]
		[Tooltip("Flat enough to read as a silhouette, light enough to keep the shape.")]
		[SerializeField] private Color m_LockedTint = new Color(0.10f, 0.10f, 0.13f, 1f);

		private RectTransform m_Rect;
		private Vector2 m_RestingPosition;
		private BirdProfile m_Bird;
		private bool m_IsUnlocked;
		private bool m_IsSelected;

		/// <summary>
		/// Raised when the player presses this card, earned or not. The screen decides what a
		/// press means, because only the screen knows what to do about a refusal.
		/// </summary>
		public event Action<BirdCard> Pressed;

		/// <summary>
		/// Raised when this card takes the focus, so the screen can sound the move. The card
		/// does not play it itself: the screen is the one that knows the difference between
		/// the player walking onto a card and the screen opening on one.
		/// </summary>
		public event Action<BirdCard> Focused;

		public BirdProfile Bird
		{
			get { return m_Bird; }
		}

		public bool IsUnlocked
		{
			get { return m_IsUnlocked; }
		}

		/// <summary>Dresses the card for one bird. Called once, as the screen is built.</summary>
		public void Show(Campaign.RosterEntry i_Entry, bool i_IsUnlocked)
		{
			m_Bird = i_Entry.Bird;
			m_IsUnlocked = i_IsUnlocked;

			if (m_Portrait != null)
			{
				m_Portrait.sprite = portraitFor(m_Bird);
				m_Portrait.color = i_IsUnlocked ? Color.white : m_LockedTint;
			}

			if (m_NameLabel != null)
			{
				m_NameLabel.text = i_IsUnlocked ? m_Bird.DisplayName : "?";
			}

			// A locked card keeps its three rows and shows a dash in each, so the habitat below
			// lands on the same line as its neighbours' and the card still reads as a bird with
			// values waiting rather than a card with a hole in it.
			setValue(m_SpeedValue, i_IsUnlocked ? m_Bird.Speed.ToString() : "-");
			setValue(m_BurstValue, i_IsUnlocked ? m_Bird.BurstRange.ToString() : "-");
			setValue(m_PodsValue, i_IsUnlocked ? m_Bird.MaxActivePods.ToString() : "-");

			if (m_HabitatLabel != null)
			{
				// A locked card's only clue, and the reason it is worth walking towards.
				m_HabitatLabel.text = i_Entry.Habitat == null ? string.Empty : i_Entry.Habitat;
			}

			if (m_LockedScrim != null)
			{
				m_LockedScrim.SetActive(!i_IsUnlocked);
			}

			if (m_Button != null)
			{
				// Deliberately interactable even when locked. Unity's navigation skips anything
				// that is not, so switching this off would not merely stop the press - it would
				// make the card invisible to the cursor and the player could never read its
				// habitat. The refusal lives in onPressed instead.
				m_Button.interactable = true;
				m_Button.onClick.AddListener(onPressed);
			}

			refreshFocus();
		}

		public void OnSelect(BaseEventData i_EventData)
		{
			m_IsSelected = true;
			refreshFocus();

			if (Focused != null)
			{
				Focused(this);
			}
		}

		public void OnDeselect(BaseEventData i_EventData)
		{
			m_IsSelected = false;
			refreshFocus();
		}

		private void OnDisable()
		{
			// A card hidden while focused never receives its deselect, so it would come back
			// lifted.
			m_IsSelected = false;
			refreshFocus();
		}

		/// <summary>
		/// Selection alone decides which card is lifted and bracketed, so exactly one card can
		/// ever look chosen.
		///
		/// Hover used to count too, and two cards could be lit at once: arrow onto one, leave
		/// the mouse resting on another, and the screen showed two answers to a question with
		/// one answer. The mouse still works - clicking a card selects it, which lands here
		/// through <see cref="OnSelect"/> - it simply no longer highlights on its own.
		/// </summary>
		private void refreshFocus()
		{
			// Captured here rather than in Awake, because the only thing that moves the card is
			// this method - so the first call always reads the true resting position, whatever
			// order the layout settled in.
			if (m_Rect == null)
			{
				m_Rect = transform as RectTransform;

				if (m_Rect != null)
				{
					m_RestingPosition = m_Rect.anchoredPosition;
				}
			}

			bool hasFocus = m_IsSelected;

			if (m_FocusBracket != null)
			{
				m_FocusBracket.SetActive(hasFocus);
			}

			if (m_Rect != null)
			{
				m_Rect.anchoredPosition = hasFocus
					? m_RestingPosition + new Vector2(0f, m_FocusLift)
					: m_RestingPosition;
			}
		}

		/// <summary>
		/// The illustrated portrait, or the bird's down-idle sprite when a profile has not been
		/// given one. A card drawing nothing is a worse failure than a card drawing small.
		/// </summary>
		private Sprite portraitFor(BirdProfile i_Bird)
		{
			if (i_Bird.CardPortrait != null)
			{
				return i_Bird.CardPortrait;
			}

			Debug.LogWarning(
				i_Bird.name + " has no card portrait, so the selection screen falls back to its "
				+ "in-game sprite.", this);

			return i_Bird.Sprites == null ? null : i_Bird.Sprites.GetIdle(eFacing.Down);
		}

		private static void setValue(TMP_Text i_Label, string i_Value)
		{
			if (i_Label != null)
			{
				i_Label.text = i_Value;
			}
		}

		private void onPressed()
		{
			Pressed?.Invoke(this);
		}
	}
}
