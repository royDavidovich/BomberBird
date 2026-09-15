using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// The screen between stages: every bird the campaign can hand out, the earned ones
	/// playable and the rest as silhouettes, and a Continue that commits the choice.
	///
	/// It shows the locked birds on purpose. At stage 2 the roster is still one bird, and the
	/// three silhouettes beside it are the screen saying there is more game here than the
	/// player has seen yet.
	///
	/// It never appears before the intro. That falls out of the routing rather than a rule:
	/// stage 1 is only reached from Play, and every later stage from CompleteStage.
	/// </summary>
	public class BirdSelectScreen : MonoBehaviour
	{
		[Header("What it reads")]
		[SerializeField] private Campaign m_Campaign;

		[Header("Parts")]
		[SerializeField] private BirdCard m_CardPrefab;
		[SerializeField] private Transform m_CardRow;
		[SerializeField] private Button m_ContinueButton;
		[SerializeField] private TMP_Text m_StageLabel;

		private readonly List<BirdCard> r_Cards = new List<BirdCard>();

		private BirdCard m_Selected;

		private void Start()
		{
			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			if (m_StageLabel != null && GameFlow.Instance != null)
			{
				m_StageLabel.text = "Stage " + GameFlow.Instance.StageNumber;
			}

			buildCards();

			if (m_ContinueButton != null)
			{
				m_ContinueButton.onClick.AddListener(Continue);
			}
		}

		/// <summary>Wired to Continue. Commits the bird, then plays the stage.</summary>
		public void Continue()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no run to choose a bird for.", this);
				return;
			}

			// SelectBird refuses a bird the run has not earned. A locked card cannot be
			// clicked, so this is the second lock rather than the only one.
			if (m_Selected != null && !GameFlow.Instance.SelectBird(m_Selected.Bird))
			{
				Debug.LogError(
					name + ": the run refused " + m_Selected.Bird.name + ", so the stage keeps the "
					+ "bird it already had.", this);
			}

			GameFlow.Instance.StartStage();
		}

		private void buildCards()
		{
			IList<Campaign.RosterEntry> roster = m_Campaign.EveryBird();
			IList<BirdProfile> earned = GameFlow.Instance == null ? null : GameFlow.Instance.Roster;
			BirdProfile playing = GameFlow.Instance == null ? null : GameFlow.Instance.SelectedBird;

			for (int i = 0; i < roster.Count; ++i)
			{
				Campaign.RosterEntry entry = roster[i];
				bool unlocked = earned != null && earned.Contains(entry.Bird);

				BirdCard card = Instantiate(m_CardPrefab, m_CardRow);
				card.name = "Card " + entry.Bird.name;
				card.Chosen += onCardChosen;
				card.Show(entry, unlocked);

				r_Cards.Add(card);

				// Open on the bird already being flown, falling back to the first playable one
				// so Continue is never pressed with nothing chosen.
				if (unlocked && (entry.Bird == playing || m_Selected == null))
				{
					select(card);
				}
			}

			// Keyboard and gamepad need something focused, or the screen cannot be driven
			// without a mouse - and Docs/GDD.md commits to a D-pad.
			if (m_Selected != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(m_Selected.gameObject);
			}
		}

		private void onCardChosen(BirdCard i_Card)
		{
			select(i_Card);
		}

		private void select(BirdCard i_Card)
		{
			for (int i = 0; i < r_Cards.Count; ++i)
			{
				r_Cards[i].SetSelected(r_Cards[i] == i_Card);
			}

			m_Selected = i_Card;
		}

		private bool hasRequiredReferences()
		{
			if (m_Campaign == null)
			{
				Debug.LogError(name + ": m_Campaign is not assigned.", this);
				return false;
			}

			if (m_CardPrefab == null)
			{
				Debug.LogError(name + ": m_CardPrefab is not assigned.", this);
				return false;
			}

			if (m_CardRow == null)
			{
				Debug.LogError(name + ": m_CardRow is not assigned.", this);
				return false;
			}

			return true;
		}
	}
}
