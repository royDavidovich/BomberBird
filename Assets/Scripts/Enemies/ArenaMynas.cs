using System.Collections.Generic;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Enemies
{
	/// <summary>
	/// Every myna in the stage: spawning them where the layout says, keeping the list of
	/// which are still alive, and killing the ones a burst catches.
	///
	/// It owns the mynas the way <see cref="PodField"/> owns the pods, so anything that
	/// needs to know where they are asks this rather than searching the scene. The stage
	/// objective will read <see cref="LivingCount"/> once there is a feather to drop.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class ArenaMynas : MonoBehaviour
	{
		[Header("Arena")]
		[Tooltip("Optional. Assign it and a placed pod blocks the mynas as well as the bird.")]
		[SerializeField] private ArenaPods m_Pods;

		[Header("Spawning")]
		[SerializeField] private MynaMovement m_Prefab;

		[Tooltip("Seed for the wandering. Leave at 0 to have every run differ.")]
		[SerializeField] private int m_RandomSeed;

		[Header("Danger")]
		[Tooltip("Seconds a burst keeps killing mynas. Keep this in step with BirdDeath.")]
		[SerializeField] private float m_LethalSeconds = 0.45f;

		private readonly List<MynaMovement> r_Living = new List<MynaMovement>();
		private readonly BurstDanger r_Danger = new BurstDanger();

		private BomberBird.Arena.Arena m_Arena;
		private PodField m_Field;
		private Vector2Int m_LastDefeatedCell;
		private int m_SpawnedCount;

		/// <summary>How many mynas are still alive. Zero is the stage objective met.</summary>
		public int LivingCount
		{
			get { return r_Living.Count; }
		}

		/// <summary>
		/// How many this stage started with. Zero means there was never anything to defeat,
		/// which is not the same as having defeated them all.
		/// </summary>
		public int SpawnedCount
		{
			get { return m_SpawnedCount; }
		}

		/// <summary>
		/// Where the most recently killed myna fell, which is where the feather drops so the
		/// player is already looking at that part of the arena.
		/// </summary>
		public Vector2Int LastDefeatedCell
		{
			get { return m_LastDefeatedCell; }
		}

		/// <summary>True when a living myna is standing on this cell.</summary>
		public bool IsMynaAt(Vector2Int i_Cell)
		{
			bool found = false;

			for (int i = 0; i < r_Living.Count && !found; ++i)
			{
				found = r_Living[i].Cell == i_Cell;
			}

			return found;
		}

		/// <summary>
		/// True when any living myna has claimed this cell, either standing on it or walking
		/// into it.
		///
		/// Separate from <see cref="IsMynaAt"/> on purpose, because they answer different
		/// questions. That one asks where a myna appears to be, which is what decides whether
		/// it kills the bird. This one asks what is spoken for, which is what decides where
		/// another myna is allowed to walk.
		/// </summary>
		private bool isClaimedByAMyna(Vector2Int i_Cell)
		{
			bool claimed = false;

			for (int i = 0; i < r_Living.Count && !claimed; ++i)
			{
				claimed = r_Living[i].Occupies(i_Cell);
			}

			return claimed;
		}

		private void Awake()
		{
			m_Arena = GetComponent<BomberBird.Arena.Arena>();

			if (m_Prefab == null)
			{
				Debug.LogError(name + ": m_Prefab is not assigned.", this);
				enabled = false;
				return;
			}

			if (m_Arena.Grid == null || m_Arena.Layout == null)
			{
				Debug.LogError(name + ": the Arena has no layout or grid.", this);
				enabled = false;
				return;
			}

			if (m_Pods != null)
			{
				m_Field = m_Pods.Field;
			}

			spawnFromLayout();
		}

		private void OnEnable()
		{
			if (m_Field != null)
			{
				m_Field.PodExploded += podField_PodExploded;
			}
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodExploded -= podField_PodExploded;
			}
		}

		private void Update()
		{
			// Backwards, so removing the myna that just died does not skip the next one.
			for (int i = r_Living.Count - 1; i >= 0; --i)
			{
				MynaMovement myna = r_Living[i];

				if (r_Danger.IsBurning(myna.Cell, Time.time))
				{
					m_LastDefeatedCell = myna.Cell;
					r_Living.RemoveAt(i);
					Destroy(myna.gameObject);
				}
			}
		}

		private void podField_PodExploded(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			r_Danger.Mark(i_Covered, Time.time, m_LethalSeconds);
		}

		private void spawnFromLayout()
		{
			// A fixed seed makes a run repeatable while the behaviour is being tuned; zero
			// leaves it to the clock so two plays of a stage do not walk identically.
			System.Random random = m_RandomSeed == 0
				? new System.Random()
				: new System.Random(m_RandomSeed);

			// Shared by every myna, and read live, so a myna deciding where to go sees the
			// cells its neighbours have already claimed this frame. A myna is never its own
			// neighbour, so it cannot block itself.
			System.Predicate<Vector2Int> alsoBlocked =
				cell => (m_Field != null && m_Field.IsBlocking(cell)) || isClaimedByAMyna(cell);

			IList<Vector2Int> cells = m_Arena.Layout.MynaSpawnCells;

			for (int i = 0; i < cells.Count; ++i)
			{
				Vector2Int cell = cells[i];

				if (!m_Arena.Grid.IsWalkable(cell))
				{
					Debug.LogError(
						name + ": the myna spawn at " + cell + " is not open floor. Check the stage layout.", this);
					continue;
				}

				MynaMovement myna = Instantiate(m_Prefab, transform);
				myna.name = "Myna " + (i + 1);
				myna.Initialise(m_Arena.Grid, cell, alsoBlocked, random);

				r_Living.Add(myna);
				++m_SpawnedCount;
			}
		}
	}
}
