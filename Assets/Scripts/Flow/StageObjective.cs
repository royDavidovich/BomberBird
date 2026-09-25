using System;
using System.Collections;
using System.Collections.Generic;
using BomberBird.Enemies;
using BomberBird.Player;
using BomberBird.Pods;
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

		[Header("What can strike the cage")]
		[Tooltip("Read so a burst that reaches the cage can be answered. It never opens it.")]
		[SerializeField] private ArenaPods m_Pods;

		[Header("The cage")]
		[Tooltip("Drawn over the feather, so its bars must be see-through.")]
		[SerializeField] private Sprite m_CageSprite;

		[Tooltip("Sorting order for the cage. Above the feather, below the birds.")]
		[SerializeField] private int m_CageSortingOrder = 6;

		[Tooltip("Seconds the bars hold before they come away. The last myna dies on the very "
			+ "frame the cage opens and its squawk runs about a third of a second, so the break "
			+ "waits for it. Picture and sound move together on this one number - split them and "
			+ "it reads as a lag rather than a beat.")]
		[SerializeField] private float m_BreakDelay = 0.25f;

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
		private int m_LastStruckFrame = -1;

		/// <summary>
		/// Raised the moment the bird walks onto the freed feather, with the cell it lay on.
		/// </summary>
		public event Action<Vector2Int> FeatherCollected;

		/// <summary>
		/// Raised when the last myna falls and the bars come away, with the cell they stood on.
		///
		/// The cage is never broken: it stops bursts and blocks movement for the whole stage,
		/// and clearing the arena is the only thing that opens it. So this is the arena giving
		/// the feather up rather than the player forcing it, and anything listening should say
		/// so.
		/// </summary>
		public event Action<Vector2Int> CageOpened;

		/// <summary>
		/// Raised whenever a burst reaches the closed cage, with the cage's cell. Nothing
		/// changes - the cage holds - but a player aiming pods at it needs to hear that it
		/// holds, or silence reads as "not enough pods yet".
		/// </summary>
		public event Action<Vector2Int> CageStruck;

		/// <summary>The cage's bars while they stand, for anything that shakes them; null once open.</summary>
		public Transform CageVisual
		{
			get { return m_CageVisual == null ? null : m_CageVisual.transform; }
		}

		/// <summary>
		/// The bird whose feather was actually picked up this stage, or null if none was.
		///
		/// The results screen cannot ask <see cref="GameFlow.AwardedBird"/> for this: the
		/// feather joins the roster the moment it is collected, well before the gate is
		/// reached, and that property reports null for a bird already held. Asking it would
		/// hide the feather on the very stage that earned it. This reads what happened rather
		/// than what is owed, and m_AwardedBird is cached in Start, so it survives the unlock.
		/// </summary>
		public BirdProfile CollectedBird
		{
			get { return m_IsFeatherCollected ? m_AwardedBird : null; }
		}

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
				listenForStrikes();
			}
		}

		/// <summary>
		/// The pods only feed the cage's answer to a strike, never the rule, so a missing
		/// reference costs the clank and the shake and leaves the stage playable.
		/// </summary>
		private void listenForStrikes()
		{
			if (m_Pods == null || m_Pods.Field == null)
			{
				Debug.LogWarning(name + ": m_Pods is not assigned, so the cage cannot answer a burst.", this);
				return;
			}

			m_Pods.Field.BurstStopped += podField_BurstStopped;
		}

		private void OnDestroy()
		{
			if (m_Pods != null && m_Pods.Field != null)
			{
				m_Pods.Field.BurstStopped -= podField_BurstStopped;
			}
		}

		/// <summary>
		/// Once per frame at most: a chain of pods ringing the cage resolves in one frame, and
		/// one clank says it as well as five stacked on top of each other.
		/// </summary>
		private void podField_BurstStopped(Vector2Int i_Origin, IList<Vector2Int> i_Stops)
		{
			Vector2Int cage = m_Arena.Grid.CageCell;

			if (m_IsCageOpen || Time.frameCount == m_LastStruckFrame || !i_Stops.Contains(cage))
			{
				return;
			}

			m_LastStruckFrame = Time.frameCount;
			OnCageStruck(cage);
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

			Vector2Int cell = m_Arena.Grid.CageCell;

			// The rule, now. The GDD has defeating the last myna open the cage, and the player
			// must be able to walk in the moment it does - only the bars coming away is held.
			m_Arena.Grid.TryOpen(cell);

			StartCoroutine(breakCage(cell));
		}

		/// <summary>
		/// The bars coming away, held back so they do not land under the squawk of the myna
		/// whose death opened them.
		///
		/// The sprite, the effect and the sound all hang off the one event raised at the end of
		/// this, so they cannot drift apart. They did once: the puff played on the frame the cage
		/// opened while the sound waited on a timer of its own, and the quarter second between
		/// them read as the game lagging rather than as a beat.
		/// </summary>
		private IEnumerator breakCage(Vector2Int i_Cell)
		{
			if (m_BreakDelay > 0f)
			{
				yield return new WaitForSeconds(m_BreakDelay);
			}

			if (m_CageVisual != null)
			{
				Destroy(m_CageVisual);
				m_CageVisual = null;
			}

			OnCageOpened(i_Cell);
		}

		private void collectFeather()
		{
			m_IsFeatherCollected = true;

			// Read before the destroy, because the feather is gone by the time anyone
			// listening gets to ask it where it was.
			Vector2Int cell = m_Feather.Cell;

			Destroy(m_Feather.gameObject);
			m_Feather = null;

			// The whole point of the pickup: the bird joins the roster here, not on a
			// results screen.
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.UnlockBird(m_AwardedBird);
			}

			OnFeatherCollected(cell);
		}

		private void OnFeatherCollected(Vector2Int i_Cell)
		{
			FeatherCollected?.Invoke(i_Cell);
		}

		private void OnCageStruck(Vector2Int i_Cell)
		{
			CageStruck?.Invoke(i_Cell);
		}

		private void OnCageOpened(Vector2Int i_Cell)
		{
			CageOpened?.Invoke(i_Cell);
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
