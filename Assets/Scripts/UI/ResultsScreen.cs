using BomberBird.Flow;
using BomberBird.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// What the player sees when a stage ends: the stage cleared, or the run out of lives.
	///
	/// It lives in the arena rather than in a scene of its own, so the counters it reports are
	/// still standing behind it and nothing has to be carried across a scene load. It listens
	/// to <see cref="GameFlow.StageEnded"/> because the flow cannot see this assembly, and it
	/// owns the freeze while it is up.
	/// </summary>
	public class ResultsScreen : MonoBehaviour
	{
		[Header("Panels")]
		[SerializeField] private GameObject m_Overlay;
		[SerializeField] private GameObject m_ClearedPanel;
		[SerializeField] private GameObject m_GameOverPanel;

		[Header("What it suspends")]
		[Tooltip("Switched off while the results are up, so Escape cannot pause on top of them.")]
		[SerializeField] private PauseController m_Pause;

		[Tooltip("Stopped by hand: a frozen clock halts movement, but pod placing reads input "
			+ "in Update and would keep working.")]
		[SerializeField] private BirdMovement m_Movement;
		[SerializeField] private BirdPodPlacer m_Placer;

		[Header("What it reads")]
		[SerializeField] private StageCounters m_Counters;
		[SerializeField] private StageObjective m_Objective;

		[Header("Cleared")]
		[SerializeField] private TMP_Text m_ClearedTitle;
		[SerializeField] private TMP_Text m_ClearedHabitat;
		[SerializeField] private TMP_Text m_ClearedMynas;
		[SerializeField] private TMP_Text m_ClearedPods;
		[SerializeField] private TMP_Text m_ClearedTime;
		[SerializeField] private Image m_ClearedPortrait;
		[SerializeField] private TMP_Text m_ClearedBirdName;

		[Tooltip("Hidden unless this stage's feather was actually picked up.")]
		[SerializeField] private GameObject m_FeatherRow;

		[Tooltip("Shown only on the campaign's last stage.")]
		[SerializeField] private GameObject m_TotalsRow;
		[SerializeField] private TMP_Text m_TotalsText;
		[SerializeField] private TMP_Text m_NextLabel;
		[SerializeField] private GameObject m_ClearedFirstSelected;

		[Header("Game over")]
		[SerializeField] private TMP_Text m_GameOverStage;
		[SerializeField] private GameObject m_GameOverFirstSelected;

		private bool m_IsShowing;

		private void OnEnable()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.StageEnded += gameFlow_StageEnded;
			}
		}

		/// <summary>
		/// Unsubscribing matters more here than usual: GameFlow outlives the scene, so a
		/// delegate left pointing at this object would be called on a destroyed component the
		/// next time any stage ended.
		/// </summary>
		private void OnDisable()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.StageEnded -= gameFlow_StageEnded;
			}
		}

		private void Start()
		{
			if (m_Overlay != null)
			{
				m_Overlay.SetActive(false);
			}
		}

		/// <summary>Wired to Next stage. Leaves this stage and plays whatever follows it.</summary>
		public void NextStage()
		{
			if (GameFlow.Instance == null)
			{
				return;
			}

			GameFlow.Instance.AdvanceToNextStage(
				m_Counters == null ? 0 : m_Counters.MynasDefeated,
				m_Counters == null ? 0 : m_Counters.PodsPlaced,
				m_Counters == null ? 0f : m_Counters.Seconds);
		}

		/// <summary>Wired to Retry on the cleared panel. Plays this stage again.</summary>
		public void RetryStage()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.RestartStage();
			}
		}

		/// <summary>
		/// Wired to Retry on game over. The lives come back; the stage reached and the birds
		/// earned do not move.
		/// </summary>
		public void RetryAfterGameOver()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.RetryAfterGameOver();
			}
		}

		/// <summary>Wired to Main menu on game over. Gives the run up.</summary>
		public void GoToMainMenu()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.GoToMainMenu();
			}
		}

		private void gameFlow_StageEnded(eStageOutcome i_Outcome)
		{
			if (m_IsShowing)
			{
				return;
			}

			m_IsShowing = true;

			if (i_Outcome == eStageOutcome.Cleared)
			{
				fillCleared();
			}
			else
			{
				fillGameOver();
			}

			show(i_Outcome == eStageOutcome.Cleared ? m_ClearedPanel : m_GameOverPanel);
			select(i_Outcome == eStageOutcome.Cleared
				? m_ClearedFirstSelected
				: m_GameOverFirstSelected);
		}

		/// <summary>
		/// Order matters. The pause controller goes first: it only restores the clock when it
		/// is actually paused, so switching it off while the game runs is silent, and a
		/// disabled component stops receiving Update, which is what keeps Escape from opening
		/// the pause overlay on top of this one.
		/// </summary>
		private void show(GameObject i_Panel)
		{
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

			if (m_ClearedPanel != null)
			{
				m_ClearedPanel.SetActive(i_Panel == m_ClearedPanel);
			}

			if (m_GameOverPanel != null)
			{
				m_GameOverPanel.SetActive(i_Panel == m_GameOverPanel);
			}

			if (m_Overlay != null)
			{
				m_Overlay.SetActive(true);
			}
		}

		private void fillCleared()
		{
			GameFlow flow = GameFlow.Instance;

			if (flow == null)
			{
				return;
			}

			bool isFinale = flow.IsFinalStage;

			setText(m_ClearedTitle, isFinale
				? "The valley is yours again"
				: "Stage " + flow.StageNumber + " cleared");

			Campaign.Stage stage = flow.CurrentStage;
			setText(m_ClearedHabitat, stage == null ? string.Empty : stage.HabitatName);

			setBird(flow.SelectedBird);
			setCounters(m_ClearedMynas, m_ClearedPods, m_ClearedTime);

			// The feather is sourced from what was picked up, not from what the stage owes:
			// by now the bird is already in the roster and GameFlow.AwardedBird reads null.
			if (m_FeatherRow != null)
			{
				m_FeatherRow.SetActive(m_Objective != null && m_Objective.CollectedBird != null);
			}

			if (m_TotalsRow != null)
			{
				m_TotalsRow.SetActive(isFinale);
			}

			if (isFinale && m_TotalsText != null)
			{
				// The stage just played has not been folded in yet, so add it here.
				m_TotalsText.text =
					(flow.TotalMynasDefeated + countMynas()) + " mynas   "
					+ (flow.TotalPodsPlaced + countPods()) + " pods   "
					+ StageCounters.FormatTime(flow.TotalSeconds + countSeconds());
			}

			setText(m_NextLabel, isFinale ? "See what you saved" : "Next stage");
		}

		private void fillGameOver()
		{
			GameFlow flow = GameFlow.Instance;

			setText(m_GameOverStage, flow == null
				? string.Empty
				: "Stage " + flow.StageNumber + " beat you");
		}

		private void setBird(BirdProfile i_Bird)
		{
			if (i_Bird == null)
			{
				return;
			}

			setText(m_ClearedBirdName, i_Bird.DisplayName);

			if (m_ClearedPortrait == null)
			{
				return;
			}

			m_ClearedPortrait.sprite = i_Bird.CardPortrait != null
				? i_Bird.CardPortrait
				: (i_Bird.Sprites == null ? null : i_Bird.Sprites.GetIdle(eFacing.Down));
		}

		private void setCounters(TMP_Text i_Mynas, TMP_Text i_Pods, TMP_Text i_Time)
		{
			setText(i_Mynas, countMynas().ToString());
			setText(i_Pods, countPods().ToString());
			setText(i_Time, StageCounters.FormatTime(countSeconds()));
		}

		private int countMynas()
		{
			return m_Counters == null ? 0 : m_Counters.MynasDefeated;
		}

		private int countPods()
		{
			return m_Counters == null ? 0 : m_Counters.PodsPlaced;
		}

		private float countSeconds()
		{
			return m_Counters == null ? 0f : m_Counters.Seconds;
		}

		private static void setText(TMP_Text i_Label, string i_Text)
		{
			if (i_Label != null)
			{
				i_Label.text = i_Text;
			}
		}

		private static void select(GameObject i_First)
		{
			if (i_First != null && EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(i_First);
			}
		}
	}
}
