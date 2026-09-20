using System;
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

		[Tooltip("The boss. Spawned only on a stage whose layout names a boss cell.")]
		[SerializeField] private MynaMovement m_BossPrefab;

		[Tooltip("Seed for the wandering. Leave at 0 to have every run differ.")]
		[SerializeField] private int m_RandomSeed;

		[Header("Easy mynas")]
		[Tooltip("What the mynas' authored speed is multiplied by in the easy mode. Applied "
			+ "to the stage's mynas, the boss and its slaves alike.")]
		[Range(0.2f, 1f)]
		[SerializeField] private float m_EasySpeedScale = 0.7f;

		[Range(0f, 1f)]
		[Tooltip("How often an easy myna walks straight through a junction instead of picking "
			+ "a way at random.")]
		[SerializeField] private float m_EasyStraightChance = 0.8f;

		[Header("Danger")]
		[Tooltip("Seconds a burst keeps killing mynas. Keep this in step with BirdDeath.")]
		[SerializeField] private float m_LethalSeconds = 0.45f;

		[Header("The boss falling")]
		[Tooltip("Seconds the slaves scatter for after the boss dies, before they are gone.")]
		[SerializeField] private float m_ScatterSeconds = 1.25f;

		[Tooltip("Speed the slaves scatter at. A little above a walk - enough to read as a "
			+ "rout, not so much that it looks like a glitch.")]
		[SerializeField] private float m_ScatterSpeed = 3f;

		// Far enough to clear a boss boxed in by pods and hard blocks, near enough that a
		// summoned slave still reads as having come from the boss.
		private const int k_MaxSummonRadius = 4;

		private readonly List<MynaMovement> r_Living = new List<MynaMovement>();
		private readonly BurstDanger r_Danger = new BurstDanger();

		private BomberBird.Arena.Arena m_Arena;
		private PodField m_Field;
		private MynaBoss m_Boss;
		private System.Random m_Random;
		private System.Predicate<Vector2Int> m_AlsoBlocked;
		private int m_SpawnedCount;

		/// <summary>
		/// Whether this stage's mynas walk the easy way: slower, and holding their line
		/// through a junction most of the time.
		///
		/// Set by <see cref="BomberBird.Flow.StageSetup"/> before the mynas are spawned,
		/// because the enemies know nothing about the run - the dependency runs the other
		/// way. A gameplay scene opened on its own leaves it false and plays as authored.
		/// </summary>
		public bool UseEasyMynas { get; set; }

		/// <summary>Raised when a burst catches a myna, with the cell it fell on.</summary>
		public event Action<Vector2Int> MynaDefeated;

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

				// The boss is never killed here. It loses a life per explosion instead, in
				// podField_PodExploded - this check runs every frame the cell is alight, so
				// leaving the boss in it would spend all five lives on a single burst.
				if (m_Boss != null && myna == m_Boss.GetComponent<MynaMovement>())
				{
					continue;
				}

				if (r_Danger.IsBurning(myna.Cell, Time.time))
				{
					Vector2Int cell = myna.Cell;

					r_Living.RemoveAt(i);
					Destroy(myna.gameObject);

					// Read before the destroy, because the myna is gone by the time anyone
					// listening gets to ask it where it was.
					OnMynaDefeated(cell);
				}
			}
		}

		private void OnMynaDefeated(Vector2Int i_Cell)
		{
			MynaDefeated?.Invoke(i_Cell);
		}

		private void podField_PodExploded(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			r_Danger.Mark(i_Covered, Time.time, m_LethalSeconds);

			hitBossIfCaught(i_Covered);
		}

		/// <summary>
		/// One explosion, one life. Driven by the event rather than by the burning cell,
		/// because a chain resolves as several PodExploded calls in the same frame and each
		/// of those should cost the boss separately, while the flames that linger afterwards
		/// should cost it nothing.
		/// </summary>
		private void hitBossIfCaught(IList<Vector2Int> i_Covered)
		{
			if (m_Boss == null || i_Covered == null)
			{
				return;
			}

			MynaMovement movement = m_Boss.GetComponent<MynaMovement>();
			bool caught = false;

			for (int i = 0; i < i_Covered.Count && !caught; ++i)
			{
				caught = i_Covered[i] == movement.Cell;
			}

			if (!caught)
			{
				return;
			}

			int slaves;

			if (m_Boss.TakeHit(out slaves) == eBossHit.Killed)
			{
				killTheBoss(movement);
				return;
			}

			summonSlaves(movement.Cell, slaves);
		}

		/// <summary>
		/// The boss falls and takes its fight with it: every slave still standing scatters
		/// and is gone.
		///
		/// They leave <see cref="r_Living"/> at once rather than when they vanish, which is
		/// what makes them harmless on the way out - <see cref="IsMynaAt"/> reads that list,
		/// and it is the only thing the bird's death check asks about touching a myna. It is
		/// also what drops <see cref="LivingCount"/> to zero on the killing blow, so the
		/// stage ends there instead of after a chase.
		/// </summary>
		private void killTheBoss(MynaMovement i_Boss)
		{
			Vector2Int cell = i_Boss.Cell;

			r_Living.Remove(i_Boss);
			Destroy(i_Boss.gameObject);
			m_Boss = null;

			for (int i = r_Living.Count - 1; i >= 0; --i)
			{
				MynaMovement slave = r_Living[i];

				r_Living.RemoveAt(i);
				slave.Speed = m_ScatterSpeed;
				Destroy(slave.gameObject, m_ScatterSeconds);
			}

			OnMynaDefeated(cell);
		}

		/// <summary>
		/// Calls this hit's slaves in beside the boss, working outward when the ring around
		/// it cannot hold them all. A wave that does not fit spawns short rather than putting
		/// a myna inside a wall.
		/// </summary>
		private void summonSlaves(Vector2Int i_Around, int i_Count)
		{
			for (int spawned = 0; spawned < i_Count; ++spawned)
			{
				Vector2Int cell;

				if (!tryFindSlaveCell(i_Around, out cell))
				{
					return;
				}

				spawnMyna(m_Prefab, cell, "Slave " + (m_SpawnedCount + 1));
			}
		}

		/// <summary>
		/// The nearest free cell to the boss, searched ring by ring. Walkable, unclaimed by
		/// another myna, not the cell a pod is sitting in, and not still alight.
		///
		/// That last one is the whole reason this searches outward. A slave is an ordinary
		/// myna and dies to a burning cell like any other, and the burst that summoned it is
		/// still burning on and around the boss - so summoning into the flames spawned the
		/// wave and killed it in the same breath, and the fight never escalated.
		/// </summary>
		private bool tryFindSlaveCell(Vector2Int i_Around, out Vector2Int o_Cell)
		{
			o_Cell = i_Around;

			for (int radius = 1; radius <= k_MaxSummonRadius; ++radius)
			{
				for (int x = -radius; x <= radius; ++x)
				{
					for (int y = -radius; y <= radius; ++y)
					{
						// Only the edge of this ring: the inside was searched already.
						if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
						{
							continue;
						}

						Vector2Int candidate = i_Around + new Vector2Int(x, y);

						if (m_Arena.Grid.IsWalkable(candidate)
							&& !m_AlsoBlocked(candidate)
							&& !r_Danger.IsBurning(candidate, Time.time))
						{
							o_Cell = candidate;
							return true;
						}
					}
				}
			}

			return false;
		}

		private void spawnFromLayout()
		{
			// A fixed seed makes a run repeatable while the behaviour is being tuned; zero
			// leaves it to the clock so two plays of a stage do not walk identically.
			m_Random = m_RandomSeed == 0
				? new System.Random()
				: new System.Random(m_RandomSeed);

			// Shared by every myna, and read live, so a myna deciding where to go sees the
			// cells its neighbours have already claimed this frame. A myna is never its own
			// neighbour, so it cannot block itself. Held as a field because the boss summons
			// more mynas long after this runs.
			m_AlsoBlocked =
				cell => (m_Field != null && m_Field.IsBlocking(cell)) || isClaimedByAMyna(cell);

			IList<Vector2Int> cells = m_Arena.Layout.MynaSpawnCells;

			for (int i = 0; i < cells.Count; ++i)
			{
				spawnMyna(m_Prefab, cells[i], "Myna " + (i + 1));
			}

			spawnBossFromLayout();
		}

		/// <summary>
		/// Puts the boss on the arena, for a stage whose layout names a cell for it. Stages
		/// that name none are every stage but the last, and spawn nothing here.
		/// </summary>
		private void spawnBossFromLayout()
		{
			IList<Vector2Int> cells = m_Arena.Layout.BossSpawnCells;

			if (cells == null || cells.Count == 0)
			{
				return;
			}

			if (m_BossPrefab == null)
			{
				Debug.LogError(
					name + ": this stage names a boss cell but m_BossPrefab is not assigned.", this);
				return;
			}

			if (cells.Count > 1)
			{
				Debug.LogWarning(
					name + ": this stage names " + cells.Count + " boss cells. Only the first is used.", this);
			}

			MynaMovement boss = spawnMyna(m_BossPrefab, cells[0], "Myna Boss");

			if (boss == null)
			{
				return;
			}

			m_Boss = boss.GetComponent<MynaBoss>();

			if (m_Boss == null)
			{
				Debug.LogError(
					name + ": the boss prefab has no MynaBoss component, so it would die to the "
					+ "first burst like an ordinary myna.", this);
			}
		}

		/// <summary>
		/// Spawns one myna on a cell and starts it walking. Shared by the stage's own mynas,
		/// the boss, and every slave the boss calls in, so all three enter the arena the same
		/// way and are counted the same way.
		/// </summary>
		private MynaMovement spawnMyna(MynaMovement i_Prefab, Vector2Int i_Cell, string i_Name)
		{
			if (!m_Arena.Grid.IsWalkable(i_Cell))
			{
				Debug.LogError(
					name + ": the myna spawn at " + i_Cell + " is not open floor. Check the stage layout.", this);
				return null;
			}

			MynaMovement myna = Instantiate(i_Prefab, transform);
			myna.name = i_Name;

			if (UseEasyMynas)
			{
				// Before Initialise, which chooses the first cell on the spot: a myna set
				// afterwards would take its opening turn at the hard difficulty.
				myna.Speed *= m_EasySpeedScale;
				myna.StraightChance = m_EasyStraightChance;

				MynaBoss boss = myna.GetComponent<MynaBoss>();

				if (boss != null)
				{
					boss.SlowTheFight(m_EasySpeedScale);
				}
			}

			myna.Initialise(m_Arena.Grid, i_Cell, m_AlsoBlocked, m_Random);

			r_Living.Add(myna);
			++m_SpawnedCount;

			return myna;
		}
	}
}
