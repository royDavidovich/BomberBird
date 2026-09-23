using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Pods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// Shows what the player needs to read mid-stage on the HUD tree: the habitat through its
	/// canopy, the stage on its sign, and the lives and pods in the hollows of its trunk.
	///
	/// It observes and nothing else. Deleting this component would not change placement,
	/// failure, or restart by a single rule.
	/// </summary>
	public class StageHud : MonoBehaviour
	{
		[Header("What it reads")]
		[SerializeField] private ArenaPods m_Pods;

		[Header("The sign")]
		[SerializeField] private TMP_Text m_StageLabel;
		[SerializeField] private TMP_Text m_HabitatLabel;

		[Header("The canopy")]
		[Tooltip("Sits behind the tree art, so the canopy's own leaves frame it.")]
		[SerializeField] private Image m_Hero;

		[Header("The trunk")]
		[Tooltip("One heart per hollow, in the order they are lost: the last one empties first.")]
		[SerializeField] private Image[] m_LifeIcons;

		[Tooltip("The whole hollow, hidden when the bird carries fewer pods than there are hollows.")]
		[SerializeField] private GameObject[] m_PodHollows;

		[Tooltip("The pod inside each hollow, in the same order as the hollows.")]
		[SerializeField] private Image[] m_PodIcons;

		[Tooltip("How faint a pod reads while it is out on the field. Low enough to tell apart "
			+ "at a glance, high enough that the player still sees it is coming back.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_SpentPodAlpha = 0.35f;

		private PodField m_Field;

		/// <summary>
		/// How pod hollow <paramref name="i_Index"/> reads for a bird carrying
		/// <paramref name="i_MaxPods"/> with <paramref name="i_Available"/> still in hand. The
		/// last hollows dim first, so the full ones always read from the top.
		/// </summary>
		public static ePodPip PodPip(int i_Index, int i_MaxPods, int i_Available)
		{
			if (i_Index >= i_MaxPods)
			{
				return ePodPip.Hidden;
			}

			return i_Index < i_Available ? ePodPip.Ready : ePodPip.Spent;
		}

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

			m_StageLabel.text = "STAGE " + StageWords.Spelled(flow.StageNumber);
			showHabitat(flow.CurrentStage);
			showLives(flow.Lives);
		}

		private void showHabitat(Campaign.Stage i_Stage)
		{
			m_HabitatLabel.text = i_Stage == null ? string.Empty : i_Stage.HabitatName.ToUpperInvariant();

			bool hasHero = i_Stage != null && i_Stage.HabitatHero != null;

			m_Hero.enabled = hasHero;

			if (hasHero)
			{
				m_Hero.sprite = i_Stage.HabitatHero;
			}
		}

		private void showLives(int i_Lives)
		{
			if (i_Lives > m_LifeIcons.Length)
			{
				Debug.LogWarning(name + ": " + i_Lives + " lives but only " + m_LifeIcons.Length
					+ " hollows, so the extra lives are not shown.", this);
			}

			for (int i = 0; i < m_LifeIcons.Length; ++i)
			{
				m_LifeIcons[i].enabled = i < i_Lives;
			}
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
			int maxPods = m_Pods.MaxActivePods;
			int available = maxPods - m_Field.ActivePodCount;

			for (int i = 0; i < m_PodHollows.Length; ++i)
			{
				ePodPip pip = PodPip(i, maxPods, available);
				Color colour = m_PodIcons[i].color;

				colour.a = pip == ePodPip.Spent ? m_SpentPodAlpha : 1f;
				m_PodIcons[i].color = colour;
				m_PodHollows[i].SetActive(pip != ePodPip.Hidden);
			}
		}

		private bool hasRequiredReferences()
		{
			string missing = string.Empty;

			if (m_Pods == null)
			{
				missing += " pods";
			}

			if (m_StageLabel == null)
			{
				missing += " stageLabel";
			}

			if (m_HabitatLabel == null)
			{
				missing += " habitatLabel";
			}

			if (m_Hero == null)
			{
				missing += " hero";
			}

			if (m_LifeIcons == null || m_LifeIcons.Length == 0 || System.Array.IndexOf(m_LifeIcons, null) >= 0)
			{
				missing += " lifeIcons";
			}

			if (m_PodHollows == null || m_PodIcons == null || m_PodHollows.Length != m_PodIcons.Length
				|| System.Array.IndexOf(m_PodHollows, null) >= 0 || System.Array.IndexOf(m_PodIcons, null) >= 0)
			{
				missing += " podHollows/podIcons (one icon per hollow)";
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

			if (m_Pods.MaxActivePods > m_PodHollows.Length)
			{
				Debug.LogWarning(name + ": the bird carries " + m_Pods.MaxActivePods + " pods but there are only "
					+ m_PodHollows.Length + " hollows, so the extra pods are not shown.", this);
			}

			return true;
		}
	}
}
