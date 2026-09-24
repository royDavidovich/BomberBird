using System.Collections;
using BomberBird.Flow;
using TMPro;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Teaches the cage where the player is looking at it, so nobody tries to bomb the
	/// feather free.
	///
	/// At the first cage stage of a run, the rule is written over the cage while the stage
	/// waits for Space, and the bars breathe to draw the eye. Every burst that reaches the
	/// cage shakes it, and the first one of the run adds a line saying why it held. The
	/// clank that goes with the shake is <see cref="StageAudio"/>'s, off the same event.
	///
	/// It observes and nothing else: deleting it leaves the cage exactly as solid.
	/// </summary>
	public class CageHint : MonoBehaviour
	{
		[Header("What it reads")]
		[SerializeField] private StageObjective m_Objective;
		[SerializeField] private BomberBird.Arena.Arena m_Arena;
		[SerializeField] private StageReady m_Ready;

		[Header("Words")]
		[Tooltip("Shown over the cage while the first cage stage of the run waits for Space.")]
		[SerializeField] private TMP_Text m_RuleLabel;

		[Tooltip("Shown over the cage the first time in the run a burst reaches it.")]
		[SerializeField] private TMP_Text m_StrikeLabel;

		[Tooltip("How far above the cage's centre the words sit, in cells.")]
		[SerializeField] private float m_LabelHeight = 1.4f;

		[Tooltip("Seconds the strike line stays up, the last of them fading out.")]
		[SerializeField] private float m_StrikeSeconds = 3f;

		[Header("The bars")]
		[Tooltip("How much bigger the cage grows at the top of each breath while the rule is up.")]
		[SerializeField] private float m_PulseGrowth = 0.08f;

		[Tooltip("Seconds for one breath, matching the start prompt's pulse.")]
		[SerializeField] private float m_PulseSeconds = 1.4f;

		[Tooltip("How far the cage swings side to side when struck, in cells.")]
		[SerializeField] private float m_ShakeDistance = 0.08f;

		[Tooltip("Seconds a strike's shake lasts.")]
		[SerializeField] private float m_ShakeSeconds = 0.25f;

		private bool m_IsDeciding;
		private bool m_IsShowingRule;
		private Coroutine m_Shake;
		private Coroutine m_StrikeFade;

		private void OnEnable()
		{
			if (m_Objective != null)
			{
				m_Objective.CageStruck += objective_CageStruck;
			}
		}

		private void OnDisable()
		{
			if (m_Objective != null)
			{
				m_Objective.CageStruck -= objective_CageStruck;
			}
		}

		private void Start()
		{
			showLabel(m_RuleLabel, false);
			showLabel(m_StrikeLabel, false);

			if (m_Arena == null || m_Arena.Grid == null || !m_Arena.Grid.HasCage)
			{
				enabled = false;
				return;
			}

			Vector3 above = m_Arena.Grid.CellToWorld(m_Arena.Grid.CageCell) + Vector3.up * m_LabelHeight;

			placeLabel(m_RuleLabel, above);
			placeLabel(m_StrikeLabel, above);

			m_IsDeciding = true;
		}

		/// <summary>
		/// Whether to show the rule is settled on the first frame rather than in Start. The bars
		/// only exist once StageObjective's own Start has placed them, which is also how a stage
		/// whose bird is already held says it has no cage to explain, and StageReady's hold may
		/// begin after this Start. By the first Update both have run.
		/// </summary>
		private void decideRule()
		{
			m_IsDeciding = false;

			GameFlow flow = GameFlow.Instance;
			bool hasBars = m_Objective != null && m_Objective.CageVisual != null;
			bool isHeld = m_Ready != null && m_Ready.IsHolding;

			m_IsShowingRule = hasBars && isHeld && flow != null && flow.ClaimCageRuleShowing();
			showLabel(m_RuleLabel, m_IsShowingRule);
		}

		/// <summary>
		/// Unscaled, because the stage it waits over holds the clock at zero.
		/// </summary>
		private void Update()
		{
			if (m_IsDeciding)
			{
				decideRule();
			}

			if (!m_IsShowingRule)
			{
				return;
			}

			Transform bars = m_Objective == null ? null : m_Objective.CageVisual;

			if (m_Ready == null || !m_Ready.IsHolding)
			{
				m_IsShowingRule = false;
				showLabel(m_RuleLabel, false);

				if (bars != null)
				{
					bars.localScale = Vector3.one;
				}

				return;
			}

			if (bars != null)
			{
				// UiPulse runs from full down to the floor and back; the cage grows as it falls.
				float breath = 1f - UiPulse.Alpha(Time.unscaledTime, m_PulseSeconds, 0f);

				bars.localScale = Vector3.one * (1f + m_PulseGrowth * breath);
			}
		}

		private void objective_CageStruck(Vector2Int i_Cell)
		{
			Transform bars = m_Objective.CageVisual;

			if (bars != null)
			{
				if (m_Shake != null)
				{
					StopCoroutine(m_Shake);
				}

				m_Shake = StartCoroutine(shake(bars, m_Arena.Grid.CellToWorld(i_Cell)));
			}

			GameFlow flow = GameFlow.Instance;

			if (m_StrikeLabel != null && flow != null && flow.ClaimCageStrikeExplained())
			{
				if (m_StrikeFade != null)
				{
					StopCoroutine(m_StrikeFade);
				}

				m_StrikeFade = StartCoroutine(showStrike());
			}
		}

		/// <summary>
		/// A side-to-side rattle that dies away, ending back on the cage's own cell so a strike
		/// can never leave the bars off their tile.
		/// </summary>
		private IEnumerator shake(Transform i_Bars, Vector3 i_Home)
		{
			float elapsed = 0f;

			while (elapsed < m_ShakeSeconds && i_Bars != null)
			{
				float left = 1f - elapsed / m_ShakeSeconds;
				float swing = Mathf.Sin(elapsed * 60f) * m_ShakeDistance * left;

				i_Bars.localPosition = i_Home + Vector3.right * swing;
				elapsed += Time.unscaledDeltaTime;

				yield return null;
			}

			if (i_Bars != null)
			{
				i_Bars.localPosition = i_Home;
			}

			m_Shake = null;
		}

		private IEnumerator showStrike()
		{
			const float k_FadeSeconds = 0.5f;

			m_StrikeLabel.alpha = 1f;
			showLabel(m_StrikeLabel, true);

			float elapsed = 0f;

			while (elapsed < m_StrikeSeconds)
			{
				float fadeFrom = m_StrikeSeconds - k_FadeSeconds;

				m_StrikeLabel.alpha = elapsed < fadeFrom ? 1f : 1f - (elapsed - fadeFrom) / k_FadeSeconds;
				elapsed += Time.unscaledDeltaTime;

				yield return null;
			}

			showLabel(m_StrikeLabel, false);
			m_StrikeFade = null;
		}

		private static void placeLabel(TMP_Text i_Label, Vector3 i_Position)
		{
			if (i_Label != null)
			{
				i_Label.transform.localPosition = i_Position;
			}
		}

		private static void showLabel(TMP_Text i_Label, bool i_IsShown)
		{
			if (i_Label != null)
			{
				i_Label.gameObject.SetActive(i_IsShown);
			}
		}
	}
}
