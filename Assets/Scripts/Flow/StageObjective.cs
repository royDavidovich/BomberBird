using BomberBird.Enemies;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The stage's goal: defeat every common myna, then collect the feather the last one
	/// drops. Only once that is done may the exit be used.
	///
	/// It holds the answer rather than the exit holding it, because the rule is about the
	/// whole arena and the exit is one tile in it. <see cref="StageExit"/> asks this.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageObjective : MonoBehaviour
	{
		[Header("What has to be beaten")]
		[SerializeField] private ArenaMynas m_Mynas;

		[Header("Who collects the feather")]
		[SerializeField] private BirdMovement m_Bird;

		[Header("The feather")]
		[Tooltip("Shimmer frames per second. Slower than the pod's pulse, which means danger.")]
		[SerializeField] private float m_FrameRate = 4f;

		[Tooltip("Sorting order. Above the arena and the exit, below the birds.")]
		[SerializeField] private int m_SortingOrder = 5;

		private BomberBird.Arena.Arena m_Arena;
		private FeatherPickup m_Feather;
		private bool m_AwardsFeather;
		private bool m_IsFeatherDropped;
		private bool m_IsFeatherCollected;

		/// <summary>Whether the bird may leave through the exit.</summary>
		public bool IsExitOpen
		{
			get
			{
				return StageGate.IsExitOpen(livingMynas(), m_AwardsFeather, m_IsFeatherCollected);
			}
		}

		private void Awake()
		{
			m_Arena = GetComponent<BomberBird.Arena.Arena>();

			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			Sprite[] frames = m_Arena.Layout.FeatherFrames;

			// A stage awards a feather only if it was authored with one and has a myna to
			// drop it. The intro and the boss award none, and an arena with nothing to
			// defeat has nowhere to drop one from.
			m_AwardsFeather = frames != null && frames.Length > 0 && m_Mynas.SpawnedCount > 0;
		}

		private void Update()
		{
			if (StageGate.IsFeatherDue(livingMynas(), m_AwardsFeather, m_IsFeatherDropped))
			{
				dropFeather();
			}

			if (m_Feather != null && !m_IsFeatherCollected && m_Bird.Cell == m_Feather.Cell)
			{
				collectFeather();
			}
		}

		private int livingMynas()
		{
			return m_Mynas == null ? 0 : m_Mynas.LivingCount;
		}

		/// <summary>
		/// The feather appears where the last myna fell, so the player is looking at the
		/// right part of the arena when it does.
		/// </summary>
		private void dropFeather()
		{
			m_IsFeatherDropped = true;

			GameObject visual = new GameObject("Feather");
			visual.transform.SetParent(transform, false);

			SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
			renderer.sortingOrder = m_SortingOrder;

			m_Feather = visual.AddComponent<FeatherPickup>();
			m_Feather.Initialise(
				m_Arena.Grid, m_Mynas.LastDefeatedCell, m_Arena.Layout.FeatherFrames, m_FrameRate);
		}

		private void collectFeather()
		{
			m_IsFeatherCollected = true;

			Destroy(m_Feather.gameObject);
			m_Feather = null;
		}

		private bool hasRequiredReferences()
		{
			if (m_Mynas == null)
			{
				Debug.LogError(name + ": m_Mynas is not assigned.", this);
				return false;
			}

			if (m_Bird == null)
			{
				Debug.LogError(name + ": m_Bird is not assigned.", this);
				return false;
			}

			if (m_Arena.Grid == null || m_Arena.Layout == null)
			{
				Debug.LogError(name + ": the Arena has no layout or grid.", this);
				return false;
			}

			return true;
		}
	}
}
