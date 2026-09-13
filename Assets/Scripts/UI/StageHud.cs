using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Pods;
using TMPro;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Shows what the player needs to read mid-stage: the pods in hand, the stage, and the
	/// lives left.
	///
	/// It observes and nothing else. Deleting this component would not change placement,
	/// failure, or restart by a single rule.
	/// </summary>
	public class StageHud : MonoBehaviour
	{
		[Header("What it reads")]
		[SerializeField] private ArenaPods m_Pods;

		[Header("Labels")]
		[SerializeField] private TMP_Text m_PodsLabel;
		[SerializeField] private TMP_Text m_StageLabel;
		[SerializeField] private TMP_Text m_LivesLabel;

		private PodField m_Field;

		private void Awake()
		{
			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			m_Field = m_Pods.Field;
		}

		private void OnEnable()
		{
			if (m_Field == null)
			{
				return;
			}

			m_Field.PodPlaced += podField_PodPlaced;
			m_Field.PodExploded += podField_PodExploded;

			refreshPods();
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced -= podField_PodPlaced;
				m_Field.PodExploded -= podField_PodExploded;
			}
		}

		/// <summary>
		/// Lives and the stage are read once. Both only ever change across a scene reload,
		/// which builds this HUD again, so there is nothing here to keep in step per frame.
		/// </summary>
		private void Start()
		{
			GameFlow flow = GameFlow.Instance;

			if (flow == null)
			{
				Debug.LogWarning(name + ": no GameFlow in the scene, so stage and lives cannot be shown.", this);
				return;
			}

			m_StageLabel.text = "Stage " + flow.StageNumber;
			m_LivesLabel.text = "Lives " + flow.Lives;
		}

		private void podField_PodPlaced(Vector2Int i_Cell)
		{
			refreshPods();
		}

		private void podField_PodExploded(Vector2Int i_Cell, IList<Vector2Int> i_Covered)
		{
			refreshPods();
		}

		private void refreshPods()
		{
			int available = m_Pods.MaxActivePods - m_Field.ActivePodCount;

			m_PodsLabel.text = "Pods " + Mathf.Max(0, available);
		}

		private bool hasRequiredReferences()
		{
			string missing = string.Empty;

			if (m_Pods == null)
			{
				missing += " pods";
			}

			if (m_PodsLabel == null)
			{
				missing += " podsLabel";
			}

			if (m_StageLabel == null)
			{
				missing += " stageLabel";
			}

			if (m_LivesLabel == null)
			{
				missing += " livesLabel";
			}

			if (missing.Length > 0)
			{
				Debug.LogError(name + ": not assigned:" + missing, this);
				return false;
			}

			if (m_Pods.Field == null)
			{
				Debug.LogError(name + ": the ArenaPods component has no field.", this);
				return false;
			}

			return true;
		}
	}
}
