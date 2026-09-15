using System;
using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// One bird on the selection screen: what it looks like, what it is called, and the three
	/// values that make it different.
	///
	/// A bird not yet earned is drawn as a flat silhouette of its own sprite rather than a
	/// placeholder, so it can never fall out of step with the bird it stands for, and it shows
	/// only the habitat that awards it. The reveal is the reward.
	///
	/// Presentation only. It reports that it was picked and knows nothing about whether the
	/// run will accept that.
	/// </summary>
	public class BirdCard : MonoBehaviour
	{
		[Header("Parts")]
		[SerializeField] private Image m_Portrait;
		[SerializeField] private TMP_Text m_NameLabel;
		[SerializeField] private TMP_Text m_StatsLabel;
		[SerializeField] private TMP_Text m_HabitatLabel;
		[SerializeField] private Button m_Button;
		[SerializeField] private GameObject m_SelectedMarker;

		[Header("Locked look")]
		[Tooltip("Flat enough to read as a silhouette, light enough to keep the shape.")]
		[SerializeField] private Color m_LockedTint = new Color(0.10f, 0.10f, 0.13f, 1f);

		private BirdProfile m_Bird;
		private bool m_IsUnlocked;

		/// <summary>Raised when the player picks this card. A locked card never raises it.</summary>
		public event Action<BirdCard> Chosen;

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

			if (m_Portrait != null && m_Bird.Sprites != null)
			{
				m_Portrait.sprite = m_Bird.Sprites.GetIdle(eFacing.Down);
				m_Portrait.color = i_IsUnlocked ? Color.white : m_LockedTint;
			}

			if (m_NameLabel != null)
			{
				m_NameLabel.text = i_IsUnlocked ? m_Bird.DisplayName : "?";
			}

			if (m_StatsLabel != null)
			{
				m_StatsLabel.text = i_IsUnlocked ? describeValues() : string.Empty;
			}

			if (m_HabitatLabel != null)
			{
				// A locked card's only clue, and the reason it is worth walking towards.
				m_HabitatLabel.text = i_Entry.Habitat == null ? string.Empty : i_Entry.Habitat;
			}

			if (m_Button != null)
			{
				m_Button.interactable = i_IsUnlocked;
				m_Button.onClick.AddListener(onClicked);
			}

			SetSelected(false);
		}

		public void SetSelected(bool i_IsSelected)
		{
			if (m_SelectedMarker != null)
			{
				m_SelectedMarker.SetActive(i_IsSelected);
			}
		}

		/// <summary>The three values Docs/GDD.md says set the birds apart.</summary>
		private string describeValues()
		{
			return "Speed " + m_Bird.Speed
				+ "   Burst " + m_Bird.BurstRange
				+ "   Pods " + m_Bird.MaxActivePods;
		}

		private void onClicked()
		{
			if (!m_IsUnlocked)
			{
				return;
			}

			Chosen?.Invoke(this);
		}
	}
}
