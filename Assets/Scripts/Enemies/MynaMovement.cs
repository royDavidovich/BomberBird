using System;
using BomberBird.Arena;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Enemies
{
	/// <summary>
	/// Walks one myna around the arena, cell to cell.
	///
	/// Unlike the bird, a myna never straddles a boundary: it picks a neighbouring cell,
	/// walks to the middle of it, and only then decides again. Deciding on the centre of a
	/// cell is what keeps a turn legible, and it means the cell a myna is dangerous on is
	/// never in doubt.
	///
	/// <see cref="ArenaMynas"/> hands it the grid rather than the Inspector, because a myna
	/// is spawned from a prefab into a stage that is already built.
	/// </summary>
	public class MynaMovement : MonoBehaviour, IGridWalker
	{
		[Tooltip("Cells per second.")]
		[SerializeField] private float m_Speed = 2f;

		[Range(0f, 1f)]
		[Tooltip("How often a junction is walked straight through rather than turned at. "
			+ "Raised by the easy mode; the ordinary game leaves it at zero.")]
		[SerializeField] private float m_StraightChance;

		// A full turn, counted down as the spin runs out: the myna passes through every
		// facing exactly once.
		private static readonly eFacing[] k_SpinOrder =
		{
			eFacing.Down,
			eFacing.Left,
			eFacing.Up,
			eFacing.Right,
		};

		private ArenaGrid m_Grid;
		private Predicate<Vector2Int> m_AlsoBlocked;
		private System.Random m_Random;
		private Vector2Int m_Cell;
		private Vector2Int m_TargetCell;
		private Vector2Int m_Direction;
		private eFacing m_Facing = eFacing.Down;
		private bool m_IsMoving;
		private float m_SpinRemaining;
		private float m_SpinQuarter;

		public eFacing Facing
		{
			get { return m_Facing; }
		}

		public bool IsMoving
		{
			get { return m_IsMoving; }
		}

		/// <summary>
		/// How fast this myna walks. Settable because the boss speeds up as it is hit, the
		/// same way <see cref="BomberBird.Player.BirdMovement.Speed"/> is set per bird.
		/// </summary>
		public float Speed
		{
			get { return m_Speed; }
			set { m_Speed = value; }
		}

		/// <summary>
		/// How often this myna carries straight on through a junction instead of picking a
		/// way at random. Zero is the ordinary patrol; the easy mode raises it so a myna can
		/// be read from across the arena.
		///
		/// Set by <see cref="ArenaMynas"/> at spawn, like <see cref="Speed"/>, rather than
		/// authored per prefab: it belongs to the run's difficulty, not to the bird.
		/// </summary>
		public float StraightChance
		{
			get { return m_StraightChance; }
			set { m_StraightChance = value; }
		}

		/// <summary>
		/// The cell the myna counts as standing on, by the same rule the bird uses: a body
		/// between two cells counts as being in the nearer one.
		///
		/// Deliberately not the cell it is walking towards. That one is up to a full cell
		/// ahead of the myna for the whole step, which would make it deadly in a cell it
		/// has not reached and harmless in the one the player can see it standing in.
		/// </summary>
		public Vector2Int Cell
		{
			get { return m_Grid == null ? m_Cell : m_Grid.WorldToCell(transform.position); }
		}

		/// <summary>
		/// True while this myna has a claim on the cell, which lasts a whole step: from the
		/// cell it left until it reaches the one it is walking into.
		///
		/// Deliberately not <see cref="Cell"/>. That one reports where the myna appears to
		/// be, which flips to the target half way through a step - so two mynas swapping
		/// places in a corridor would each see the other's cell go free and walk straight
		/// through one another. Holding both ends of the step is what stops that, and what
		/// stops two mynas reserving the same empty cell from opposite sides on one frame.
		/// </summary>
		public bool Occupies(Vector2Int i_Cell)
		{
			return i_Cell == m_Cell || i_Cell == m_TargetCell;
		}

		/// <summary>
		/// Places the myna on a cell and starts it walking. Until this is called the myna
		/// has no arena and stands still.
		/// </summary>
		public void Initialise(
			ArenaGrid i_Grid, Vector2Int i_Cell, Predicate<Vector2Int> i_AlsoBlocked, System.Random i_Random)
		{
			if (i_Grid == null)
			{
				throw new ArgumentNullException("i_Grid");
			}

			if (i_Random == null)
			{
				throw new ArgumentNullException("i_Random");
			}

			m_Grid = i_Grid;
			m_AlsoBlocked = i_AlsoBlocked;
			m_Random = i_Random;
			m_Cell = i_Cell;
			m_TargetCell = i_Cell;
			m_Direction = Vector2Int.zero;

			transform.position = m_Grid.CellToWorld(i_Cell);

			chooseNextCell();
		}

		/// <summary>
		/// Holds position and turns a full circle, then walks on. The boss does this when a
		/// burst costs it a life, so the hit is legible without stopping the fight.
		///
		/// There is no animation here: <see cref="BomberBird.Player.BirdAnimator"/> draws
		/// whatever <see cref="Facing"/> says, so stepping the facing through its four
		/// values while the walk is held is the spin.
		///
		/// It starts wherever the myna is, mid-step included. Waiting to reach a cell centre
		/// would delay the only feedback the player gets by up to a whole step.
		/// </summary>
		public void Spin(float i_Seconds)
		{
			if (i_Seconds <= 0f)
			{
				return;
			}

			m_SpinRemaining = i_Seconds;
			m_SpinQuarter = i_Seconds / 4f;
		}

		private void Update()
		{
			if (m_Grid == null)
			{
				return;
			}

			if (m_SpinRemaining > 0f)
			{
				advanceSpin();
				return;
			}

			Vector3 target = m_Grid.CellToWorld(m_TargetCell);
			Vector3 from = transform.position;
			Vector3 next = Vector3.MoveTowards(from, target, m_Speed * Time.deltaTime);

			m_IsMoving = next != from;
			transform.position = next;

			if (next == target)
			{
				m_Cell = m_TargetCell;
				chooseNextCell();
			}
		}

		/// <summary>
		/// One frame of the turn. The quarter the myna is on comes from how much of the spin
		/// is left, so the rotation is even however long the spin was set to.
		/// </summary>
		private void advanceSpin()
		{
			m_SpinRemaining -= Time.deltaTime;

			// Standing still: the animator draws the idle frame for whichever way it faces,
			// which is what makes the turn read as a turn rather than a slide.
			m_IsMoving = false;

			if (m_SpinRemaining > 0f)
			{
				int quarter = m_SpinQuarter <= 0f ? 0 : (int)(m_SpinRemaining / m_SpinQuarter);

				m_Facing = k_SpinOrder[Mathf.Clamp(quarter, 0, k_SpinOrder.Length - 1)];

				return;
			}

			m_SpinRemaining = 0f;

			// Face the way it is about to walk, not wherever the rotation happened to stop.
			m_Facing = Facings.FromStep(m_Direction, m_Facing);
		}

		private void chooseNextCell()
		{
			m_Direction = MynaWalk.ChooseDirection(
				m_Grid, m_Cell, m_Direction, m_AlsoBlocked, m_Random, m_StraightChance);

			// Walled in on every side: stand still and try again next frame, because a pod
			// that bursts nearby can open the way back up.
			m_TargetCell = m_Cell + m_Direction;
			m_Facing = Facings.FromStep(m_Direction, m_Facing);
		}
	}
}
