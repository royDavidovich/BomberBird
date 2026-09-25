using System;
using System.Collections;
using System.Collections.Generic;
using BomberBird.Player;
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
		[SerializeField] private float m_EasySpeedScale = 0.5f;

		[Range(0f, 1f)]
		[Tooltip("How often an easy myna walks straight through a junction instead of picking "
			+ "a way at random.")]
		[SerializeField] private float m_EasyStraightChance = 0.8f;

		[Header("Danger")]
		[Tooltip("Seconds a burst keeps killing mynas. Keep this in step with BirdDeath.")]
		[SerializeField] private float m_LethalSeconds = 0.45f;

		[Header("The boss summoning")]
		[Tooltip("The bird, so the mynas the boss calls in appear away from it rather than on it.")]
		[SerializeField] private BirdMovement m_Bird;

		[Tooltip("Walking steps from the bird a summoned helper must appear at least.")]
		[SerializeField] private int m_SummonClearance = 6;

		[Tooltip("Seconds a summoned helper is harmless for after it appears. It blinks until then.")]
		[SerializeField] private float m_SummonGrace = 1f;

		[Tooltip("Times a second a harmless helper flips between shown and hidden, the same "
			+ "measure as BirdDeath's flash.")]
		[SerializeField] private float m_SummonBlinkRate = 10f;

		[Tooltip("Seconds a summoned helper takes to spin out of the boss to its cell, turning one "
			+ "full circle on the way. Harmless, and out of reach of the flames, until it lands; "
			+ "the blinking grace starts then.")]
		[SerializeField] private float m_SummonFlightSeconds = 0.5f;

		[Tooltip("How high, in cells, the way out arcs above the straight line from the boss. "
			+ "0 is a straight slide.")]
		[SerializeField] private float m_SummonArcHeight;

		[Tooltip("Added to a helper's sorting order on its way out, so it passes over the walls "
			+ "rather than behind them.")]
		[SerializeField] private int m_SummonFlightSortingBoost = 20;

		[Header("The boss falling")]
		[Tooltip("Seconds the slaves spin on the spot, blinking, after the boss dies, before they "
			+ "are gone.")]
		[SerializeField] private float m_ScatterSeconds = 1.25f;

		[Tooltip("Full turns each slave spins through in that time. Several, so it reads as "
			+ "reeling rather than turning to look.")]
		[SerializeField] private int m_ScatterTurns = 5;

		// How long a spin outlasts the flight or reel it covers. Both count Update's deltaTime,
		// so the margin holds at any frame rate.
		private const float k_SpinPastLanding = 0.1f;

		private readonly List<MynaMovement> r_Living = new List<MynaMovement>();
		private readonly BurstDanger r_Danger = new BurstDanger();
		private readonly Dictionary<MynaMovement, float> r_HarmlessUntil = new Dictionary<MynaMovement, float>();
		private readonly List<SpriteRenderer> r_BlinkRenderers = new List<SpriteRenderer>();

		// A helper on its way out of the boss, and the cell it is going to.
		private readonly Dictionary<MynaMovement, Vector2Int> r_Flying = new Dictionary<MynaMovement, Vector2Int>();

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

		/// <summary>
		/// True when a living myna that can hurt the bird is standing on this cell. A slave
		/// still in its first moments is left out, so one the bird walks into before it has
		/// finished blinking in does not end the run.
		/// </summary>
		public bool IsMynaAt(Vector2Int i_Cell)
		{
			bool found = false;

			for (int i = 0; i < r_Living.Count && !found; ++i)
			{
				found = r_Living[i].Cell == i_Cell && !isHarmless(r_Living[i]);
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

			if (m_Bird == null)
			{
				// Not fatal: the boss still summons, only without keeping clear of the bird.
				Debug.LogError(name + ": m_Bird is not assigned, so a summoned myna may appear on the bird.", this);
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

				// Still on its way out: not standing in any cell the flames could reach, and not
				// yet blinking, because its grace starts when it lands.
				if (r_Flying.ContainsKey(myna))
				{
					continue;
				}

				if (r_Danger.IsBurning(myna.Cell, Time.time))
				{
					Vector2Int cell = myna.Cell;

					r_Living.RemoveAt(i);
					r_HarmlessUntil.Remove(myna);
					Destroy(myna.gameObject);

					// Read before the destroy, because the myna is gone by the time anyone
					// listening gets to ask it where it was.
					OnMynaDefeated(cell);
					continue;
				}

				blinkWhileHarmless(myna);
			}
		}

		private bool isHarmless(MynaMovement i_Myna)
		{
			float until;

			return r_HarmlessUntil.TryGetValue(i_Myna, out until)
				&& (Time.time < until || isUnderTheBird(i_Myna));
		}

		/// <summary>
		/// Blinking told the player this one is safe, so it stays safe for as long as the bird
		/// is still standing in it: turning lethal under the bird would be the same unfair death
		/// the grace exists to prevent, only a second later.
		/// </summary>
		private bool isUnderTheBird(MynaMovement i_Myna)
		{
			return m_Bird != null && m_Bird.Cell == i_Myna.Cell;
		}

		/// <summary>
		/// Flashes a newly summoned slave for as long as it is harmless, so the player can see
		/// which mynas cannot hurt them yet. Once the grace is over it is left drawn and
		/// forgotten, and from then on it is an ordinary myna.
		/// </summary>
		private void blinkWhileHarmless(MynaMovement i_Myna)
		{
			float until;

			if (!r_HarmlessUntil.TryGetValue(i_Myna, out until))
			{
				return;
			}

			float remaining = until - Time.time;

			if (remaining <= 0f && !isUnderTheBird(i_Myna))
			{
				r_HarmlessUntil.Remove(i_Myna);
				setDrawn(i_Myna, true);
				return;
			}

			setDrawn(i_Myna, Mathf.FloorToInt(remaining * m_SummonBlinkRate) % 2 == 0);
		}

		private void setDrawn(MynaMovement i_Myna, bool i_IsDrawn)
		{
			i_Myna.GetComponentsInChildren(r_BlinkRenderers);

			for (int i = 0; i < r_BlinkRenderers.Count; ++i)
			{
				r_BlinkRenderers[i].enabled = i_IsDrawn;
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
		/// The boss falls and takes its fight with it: every slave still standing reels on the
		/// spot, spinning and blinking, and is gone.
		///
		/// They leave <see cref="r_Living"/> at once rather than when they vanish, which is
		/// what makes them harmless while they go - <see cref="IsMynaAt"/> reads that list,
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

				// Its grace is over, and one still on its way out is set down on its cell, so the
				// reeling happens where the player can see it end.
				r_HarmlessUntil.Remove(slave);
				land(slave);

				// Past the vanishing, so it never gets to walk off between the two. The coroutine's
				// first step runs inside StartCoroutine and draws it, so one caught mid-blink is
				// shown again at once.
				slave.Spin(m_ScatterSeconds + k_SpinPastLanding, m_ScatterTurns);
				StartCoroutine(reelAndVanish(slave));
			}

			OnMynaDefeated(cell);
		}

		/// <summary>
		/// Blinks a slave left behind by the fallen boss while it spins, then removes it.
		/// </summary>
		private IEnumerator reelAndVanish(MynaMovement i_Slave)
		{
			for (float elapsed = 0f; elapsed < m_ScatterSeconds; elapsed += Time.deltaTime)
			{
				if (i_Slave == null)
				{
					yield break;
				}

				setDrawn(i_Slave, Mathf.FloorToInt(elapsed * m_SummonBlinkRate) % 2 == 0);

				yield return null;
			}

			if (i_Slave != null)
			{
				Destroy(i_Slave.gameObject);
			}
		}

		/// <summary>
		/// Calls this hit's slaves in near the boss but away from the bird, working outward
		/// when the cells around the boss cannot hold them all. A wave that does not fit
		/// spawns short rather than putting a myna inside a wall.
		///
		/// Each one spins out of the boss to its cell, so the wave reads as hers, and is harmless
		/// until it has landed and blinked for a moment. The bird has usually just hit the boss
		/// from close by, and a myna appearing on or beside it used to kill it before the player
		/// could have seen it coming.
		/// </summary>
		private void summonSlaves(Vector2Int i_Around, int i_Count)
		{
			Vector3 from = m_Arena.Grid.CellToWorld(i_Around);

			for (int spawned = 0; spawned < i_Count; ++spawned)
			{
				Vector2Int cell;

				if (!tryFindSlaveCell(i_Around, out cell))
				{
					return;
				}

				MynaMovement slave = spawnMyna(m_Prefab, cell, "Slave " + (m_SpawnedCount + 1));

				if (slave != null)
				{
					r_HarmlessUntil[slave] = Time.time + m_SummonFlightSeconds + m_SummonGrace;
					StartCoroutine(flyOut(slave, from, cell));
				}
			}
		}

		/// <summary>
		/// Carries a slave from the boss to the cell it was spawned for, spinning the way the
		/// boss does when hit, so the escort reads as thrown out of her. The cell is already
		/// claimed, because the slave was spawned on it; only the drawing travels.
		///
		/// A spinning myna holds its walk, which is what keeps it from stepping off mid-air. The
		/// spin runs a moment past the landing so it cannot run out first.
		/// </summary>
		private IEnumerator flyOut(MynaMovement i_Slave, Vector3 i_From, Vector2Int i_Cell)
		{
			Vector3 to = m_Arena.Grid.CellToWorld(i_Cell);

			r_Flying[i_Slave] = i_Cell;
			i_Slave.Spin(m_SummonFlightSeconds + k_SpinPastLanding);
			boostSorting(i_Slave, m_SummonFlightSortingBoost);

			for (float elapsed = 0f; elapsed < m_SummonFlightSeconds; elapsed += Time.deltaTime)
			{
				// Landed early by the boss falling, or gone with the stage.
				if (i_Slave == null || !r_Flying.ContainsKey(i_Slave))
				{
					yield break;
				}

				i_Slave.transform.position = SummonArcPosition(i_From, to, m_SummonArcHeight, elapsed / m_SummonFlightSeconds);

				yield return null;
			}

			if (i_Slave != null)
			{
				land(i_Slave);
			}
		}

		/// <summary>Sets a slave on its way out down on its cell. Nothing for one on foot.</summary>
		private void land(MynaMovement i_Slave)
		{
			Vector2Int cell;

			if (!r_Flying.TryGetValue(i_Slave, out cell))
			{
				return;
			}

			r_Flying.Remove(i_Slave);
			i_Slave.transform.position = m_Arena.Grid.CellToWorld(cell);
			boostSorting(i_Slave, -m_SummonFlightSortingBoost);
		}

		private void boostSorting(MynaMovement i_Myna, int i_By)
		{
			i_Myna.GetComponentsInChildren(r_BlinkRenderers);

			for (int i = 0; i < r_BlinkRenderers.Count; ++i)
			{
				r_BlinkRenderers[i].sortingOrder += i_By;
			}
		}

		/// <summary>
		/// Where a summoned slave is drawn a fraction <paramref name="i_T"/> of the way through
		/// its flight: along the straight line from the boss to its cell, lifted by a parabola
		/// that peaks <paramref name="i_Height"/> above it halfway and is back on the line at
		/// both ends.
		/// </summary>
		public static Vector3 SummonArcPosition(Vector3 i_From, Vector3 i_To, float i_Height, float i_T)
		{
			float t = Mathf.Clamp01(i_T);

			return Vector3.Lerp(i_From, i_To, t) + Vector3.up * (i_Height * 4f * t * (1f - t));
		}

		/// <summary>
		/// A free cell for a slave: walkable, unclaimed by another myna, not the cell a pod is
		/// sitting in, and not still alight.
		///
		/// That last one is the reason the search leaves the boss's own cell. A slave is an
		/// ordinary myna and dies to a burning cell like any other, and the burst that
		/// summoned it is still burning on and around the boss - so summoning into the flames
		/// spawned the wave and killed it in the same breath, and the fight never escalated.
		///
		/// The search may reach across the whole arena, because keeping clear of the bird can
		/// push a slave well away from the boss.
		/// </summary>
		private bool tryFindSlaveCell(Vector2Int i_Around, out Vector2Int o_Cell)
		{
			Vector2Int bird = m_Bird != null ? m_Bird.Cell : i_Around;
			int clearance = m_Bird != null ? m_SummonClearance : 0;
			int maxRadius = Mathf.Max(m_Arena.Grid.Width, m_Arena.Grid.Height);

			return ChooseSummonCell(
				i_Around,
				bird,
				clearance,
				maxRadius,
				cell => m_Arena.Grid.IsWalkable(cell)
					&& !m_AlsoBlocked(cell)
					&& !r_Danger.IsBurning(cell, Time.time),
				out o_Cell);
		}

		/// <summary>
		/// Picks where a summoned slave appears: the open cell nearest the boss that is at
		/// least <paramref name="i_MinFromBird"/> walking steps from the bird, searched ring by
		/// ring out to <paramref name="i_MaxRadius"/>. Nearest the boss wins so a slave still
		/// reads as hers.
		///
		/// When the bird is so placed that no open cell is far enough, the open cell farthest
		/// from it is used instead, because a boss hit must never go unanswered just because
		/// the bird stood close. False only when no open cell but the bird's own is left.
		/// </summary>
		public static bool ChooseSummonCell(
			Vector2Int i_Boss,
			Vector2Int i_Bird,
			int i_MinFromBird,
			int i_MaxRadius,
			Predicate<Vector2Int> i_IsOpen,
			out Vector2Int o_Cell)
		{
			bool foundOpen = false;
			int farthest = -1;

			o_Cell = i_Boss;

			for (int radius = 1; radius <= i_MaxRadius; ++radius)
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

						Vector2Int candidate = i_Boss + new Vector2Int(x, y);

						if (!i_IsOpen(candidate))
						{
							continue;
						}

						int fromBird = Mathf.Abs(candidate.x - i_Bird.x) + Mathf.Abs(candidate.y - i_Bird.y);

						if (fromBird >= i_MinFromBird)
						{
							o_Cell = candidate;
							return true;
						}

						// Strictly farther, so among equals the one nearer the boss is kept. Never
						// the bird's own cell, however little else is open.
						if (fromBird > farthest && fromBird > 0)
						{
							farthest = fromBird;
							o_Cell = candidate;
							foundOpen = true;
						}
					}
				}
			}

			return foundOpen;
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
			// HasPodAt, not IsBlocking. IsBlocking exists for the bird: a pod it just placed
			// stays passable until it has stepped off, or it would wall itself in. A myna
			// placed nothing and gets no such grace, so a pod stops it from the moment it
			// lands. A myna already walking into that cell still finishes the step - the
			// choice is only made on arrival - and then cannot come back.
			m_AlsoBlocked =
				cell => (m_Field != null && m_Field.HasPodAt(cell)) || isClaimedByAMyna(cell);

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
