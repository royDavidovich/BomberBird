using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// The screen between stages: every bird the campaign can hand out, the earned ones
	/// playable and the rest as silhouettes.
	///
	/// Choosing is the whole interaction. There is no Continue to press afterwards - the player
	/// moves onto a card and presses it, and the stage starts. A locked card can be moved onto
	/// and read, but pressing it refuses.
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
		[SerializeField] private RectTransform m_CardRow;
		[SerializeField] private TMP_Text m_StageLabel;

		[Tooltip("Gap between cards. The row is laid out here rather than by a layout group, "
			+ "because a layout group rewrites every child's position on each rebuild and would "
			+ "undo the lift a focused card gives itself.")]
		[SerializeField] private float m_CardSpacing = 36f;

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
		}

		/// <summary>
		/// Commits the bird and plays the stage. Public because it is the screen's whole job,
		/// and because the play-test drives it without a mouse.
		/// </summary>
		public void Choose(BirdCard i_Card)
		{
			if (i_Card == null)
			{
				return;
			}

			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no run to choose a bird for.", this);
				return;
			}

			if (!i_Card.IsUnlocked)
			{
				// Not an error. Pressing a bird you have not earned is a thing players do on
				// purpose, to find out what it would take.
				return;
			}

			// SelectBird refuses a bird the run has not earned. The card's own lock is the
			// mechanism, so this is the second lock rather than the only one.
			if (!GameFlow.Instance.SelectBird(i_Card.Bird))
			{
				Debug.LogError(
					name + ": the run refused " + i_Card.Bird.name + ", so the stage keeps the "
					+ "bird it already had.", this);
			}

			GameFlow.Instance.StartStage();
		}

		private void buildCards()
		{
			IList<Campaign.RosterEntry> roster = m_Campaign.EveryBird();
			IList<BirdProfile> earned = GameFlow.Instance == null ? null : GameFlow.Instance.Roster;
			BirdProfile playing = GameFlow.Instance == null ? null : GameFlow.Instance.SelectedBird;
			BirdCard opensOn = null;

			RectTransform prefabRect = m_CardPrefab.transform as RectTransform;
			float cardWidth = prefabRect == null ? 0f : prefabRect.rect.width;
			float step = cardWidth + m_CardSpacing;
			float firstX = -step * (roster.Count - 1) * 0.5f;

			for (int i = 0; i < roster.Count; ++i)
			{
				Campaign.RosterEntry entry = roster[i];
				bool unlocked = earned != null && earned.Contains(entry.Bird);

				BirdCard card = Instantiate(m_CardPrefab, m_CardRow);
				card.name = "Card " + entry.Bird.name;
				place(card, firstX + step * i);
				card.Pressed += Choose;
				card.Show(entry, unlocked);

				// Open on the bird already being flown, falling back to the first playable one,
				// so the first press is never on a bird the player cannot fly.
				if (unlocked && (entry.Bird == playing || opensOn == null))
				{
					opensOn = card;
				}
			}

			// Keyboard and gamepad need something focused, or the screen cannot be driven
			// without a mouse - and Docs/GDD.md commits to a D-pad. With no Continue to fall
			// back on, an unfocused screen would not merely look odd, it would be a dead end.
			if (opensOn != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(opensOn.gameObject);
			}
		}

		/// <summary>
		/// Centres the card on the row at the given offset. Called before Show, so the resting
		/// position the card caches is the one it will return to when it loses focus.
		/// </summary>
		private void place(BirdCard i_Card, float i_X)
		{
			RectTransform rect = i_Card.transform as RectTransform;

			if (rect == null)
			{
				return;
			}

			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.anchoredPosition = new Vector2(i_X, 0f);
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
