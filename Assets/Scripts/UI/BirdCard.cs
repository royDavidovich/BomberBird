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
	public class BirdCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
		ISelectHandler, IDeselectHandler
	{
		[Header("Parts")]
		[SerializeField] private Image m_Portrait;
		[SerializeField] private TMP_Text m_NameLabel;
		[SerializeField] private TMP_Text m_StatsLabel;
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
		private bool m_IsPointerInside;
		private bool m_IsSelected;

		/// <summary>
		/// Raised when the player presses this card, earned or not. The screen decides what a
		/// press means, because only the screen knows what to do about a refusal.
		/// </summary>
		public event Action<BirdCard> Pressed;

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

			if (m_StatsLabel != null)
			{
				// A locked card keeps an empty stats line rather than dropping it, so the
				// habitat below lands on the same row as its neighbours'.
				m_StatsLabel.text = i_IsUnlocked ? describeValues() : string.Empty;
			}

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

		public void OnPointerEnter(PointerEventData i_EventData)
		{
			m_IsPointerInside = true;
			refreshFocus();
		}

		public void OnPointerExit(PointerEventData i_EventData)
		{
			m_IsPointerInside = false;
			refreshFocus();
		}

		public void OnSelect(BaseEventData i_EventData)
		{
			m_IsSelected = true;
			refreshFocus();
		}

		public void OnDeselect(BaseEventData i_EventData)
		{
			m_IsSelected = false;
			refreshFocus();
		}

		private void OnDisable()
		{
			// A card hidden mid-hover never receives its exit, so it would come back lifted.
			m_IsPointerInside = false;
			m_IsSelected = false;
			refreshFocus();
		}

		/// <summary>
		/// The pointer and the keyboard overlap rather than replace each other - hover one card,
		/// arrow-key to another, then move the mouse away - so they are tracked apart and the
		/// card answers to either. Same reasoning as <see cref="ButtonFocusArrow"/>.
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

			bool hasFocus = m_IsPointerInside || m_IsSelected;

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

		/// <summary>The three values Docs/GDD.md says set the birds apart.</summary>
		private string describeValues()
		{
			return "Speed " + m_Bird.Speed
				+ "   Burst " + m_Bird.BurstRange
				+ "   Pods " + m_Bird.MaxActivePods;
		}

		private void onPressed()
		{
			Pressed?.Invoke(this);
		}
	}
}
