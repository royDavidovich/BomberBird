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

		private ArenaGrid m_Grid;
		private Predicate<Vector2Int> m_AlsoBlocked;
		private System.Random m_Random;
		private Vector2Int m_Cell;
		private Vector2Int m_TargetCell;
		private Vector2Int m_Direction;
		private eFacing m_Facing = eFacing.Down;
		private bool m_IsMoving;

		public eFacing Facing
		{
			get { return m_Facing; }
		}

		public bool IsMoving
		{
			get { return m_IsMoving; }
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

		private void Update()
		{
			if (m_Grid == null)
			{
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

		private void chooseNextCell()
		{
			m_Direction = MynaWalk.ChooseDirection(m_Grid, m_Cell, m_Direction, m_AlsoBlocked, m_Random);

			// Walled in on every side: stand still and try again next frame, because a pod
			// that bursts nearby can open the way back up.
			m_TargetCell = m_Cell + m_Direction;
			m_Facing = Facings.FromStep(m_Direction, m_Facing);
		}
	}
}
