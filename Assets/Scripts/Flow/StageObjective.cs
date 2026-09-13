using BomberBird.Enemies;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The stage's goal: defeat every common myna, then collect the feather they were
	/// guarding. Only once that is done may the gate be used.
	///
	/// The feather is caged in plain sight from the moment the stage loads, so the player
	/// can see the reward and the bars around it for the whole level. The cage is part of
	/// the arena - solid, and proof against anything the player aims at it - so clearing
	/// the arena is the only thing that opens it.
	///
	/// It holds the answer rather than the gate holding it, because the rule is about the
	/// whole arena and the gate is one tile in it. <see cref="StageExit"/> asks this.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageObjective : MonoBehaviour
	{
		[Header("What has to be beaten")]
		[SerializeField] private ArenaMynas m_Mynas;

		[Header("Who collects the feather")]
		[SerializeField] private BirdMovement m_Bird;

		[Header("The cage")]
		[Tooltip("Drawn over the feather, so its bars must be see-through.")]
		[SerializeField] private Sprite m_CageSprite;

		[Tooltip("Sorting order for the cage. Above the feather, below the birds.")]
		[SerializeField] private int m_CageSortingOrder = 6;

		[Header("The feather")]
		[Tooltip("Shimmer frames per second. Slower than the pod's pulse, which means danger.")]
		[SerializeField] private float m_FrameRate = 4f;

		[Tooltip("Sorting order for the feather. Above the arena and the gate, below the cage.")]
		[SerializeField] private int m_SortingOrder = 5;

		private BomberBird.Arena.Arena m_Arena;
		private FeatherPickup m_Feather;
		private GameObject m_CageVisual;
		private BirdProfile m_AwardedBird;
		private bool m_AwardsFeather;
		private bool m_IsCageOpen;
		private bool m_IsFeatherCollected;

		/// <summary>Whether the bird may leave through the gate.</summary>
		public bool IsExitOpen
		{
			get
			{
				return StageGate.IsExitOpen(livingMynas(), m_AwardsFeather, m_IsFeatherCollected);
			}
		}

		/// <summary>
		/// Set up in Start rather than Awake. This reads how many mynas the stage spawned,
		/// and <see cref="ArenaMynas"/> spawns them in its own Awake - so asking any earlier
		/// makes the answer depend on the order the components happen to sit in on the
		/// GameObject, which is exactly the silent breakage the arena already avoids by
		/// building its grid lazily.
		/// </summary>
		private void Start()
		{
			m_Arena = GetComponent<BomberBird.Arena.Arena>();

			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			// Read once. The run stops offering the bird the moment it is collected, so
			// asking again mid-stage would quietly reopen the gate's condition.
			m_AwardedBird = GameFlow.Instance == null ? null : GameFlow.Instance.AwardedBird;

			bool hasFrames = m_AwardedBird != null
				&& m_AwardedBird.FeatherFrames != null
				&& m_AwardedBird.FeatherFrames.Length > 0;

			// A stage awards a feather only if the campaign names a bird for it, that bird
			// has frames to show, the map holds a cage to keep it in, and there is a myna to
			// guard it. The intro and the boss name none and carry none.
			m_AwardsFeather = hasFrames && m_Arena.Grid.HasCage && m_Mynas.SpawnedCount > 0;

			reportMismatchedStage(hasFrames);

			if (m_AwardsFeather)
			{
				placeCagedFeather();
			}
		}

		private void Update()
		{
			if (StageGate.IsFeatherDue(livingMynas(), m_AwardsFeather, m_IsCageOpen))
			{
				openCage();
			}

			if (m_Feather != null && m_IsCageOpen && !m_IsFeatherCollected
				&& m_Bird.Cell == m_Feather.Cell)
			{
				collectFeather();
			}
		}

		private int livingMynas()
		{
			return m_Mynas == null ? 0 : m_Mynas.LivingCount;
		}

		/// <summary>
		/// Puts the feather in its cage at the start of the stage. The player sees both for
		/// the whole level, which is the point: the objective is something to walk toward
		/// rather than something announced after the last myna falls.
		/// </summary>
		private void placeCagedFeather()
		{
			Vector2Int cell = m_Arena.Grid.CageCell;

			GameObject featherVisual = new GameObject("Feather");
			featherVisual.transform.SetParent(transform, false);

			SpriteRenderer featherRenderer = featherVisual.AddComponent<SpriteRenderer>();
			featherRenderer.sortingOrder = m_SortingOrder;

			m_Feather = featherVisual.AddComponent<FeatherPickup>();
			m_Feather.Initialise(m_Arena.Grid, cell, m_AwardedBird.FeatherFrames, m_FrameRate);

			m_CageVisual = new GameObject("Cage");
			m_CageVisual.transform.SetParent(transform, false);
			m_CageVisual.transform.localPosition = m_Arena.Grid.CellToWorld(cell);

			SpriteRenderer cageRenderer = m_CageVisual.AddComponent<SpriteRenderer>();
			cageRenderer.sprite = m_CageSprite;
			cageRenderer.sortingOrder = m_CageSortingOrder;
		}

		/// <summary>
		/// The last myna is down, so the cage opens where it stands. The cell stops blocking
		/// and the bars come away, leaving the feather on open floor.
		/// </summary>
		private void openCage()
		{
			m_IsCageOpen = true;

			m_Arena.Grid.TryOpen(m_Arena.Grid.CageCell);

			if (m_CageVisual != null)
			{
				Destroy(m_CageVisual);
				m_CageVisual = null;
			}
		}

		private void collectFeather()
		{
			m_IsFeatherCollected = true;

			Destroy(m_Feather.gameObject);
			m_Feather = null;

			// The whole point of the pickup: the bird joins the roster here, not on a
			// results screen.
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.UnlockBird(m_AwardedBird);
			}
		}

		/// <summary>
		/// A campaign that promises a bird and a map that holds no cage disagree about what
		/// this stage is, and so does the reverse. Either is a level-design mistake worth
		/// saying loudly when the stage loads rather than discovering as a stage that cannot
		/// be finished.
		/// </summary>
		private void reportMismatchedStage(bool i_HasFrames)
		{
			if (i_HasFrames && !m_Arena.Grid.HasCage)
			{
				Debug.LogError(
					name + ": this stage awards " + m_AwardedBird.name + " but its map has no cage "
					+ "('c') to keep the feather in, so the bird can never be earned here.", this);
			}
			else if (!i_HasFrames && m_Arena.Grid.HasCage)
			{
				Debug.LogWarning(
					name + ": this stage's map holds a cage but the campaign awards no bird here, "
					+ "so the cage will sit empty.", this);
			}
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
