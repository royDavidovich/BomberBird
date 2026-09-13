using System.Collections;
using BomberBird.Arena;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The gate in the arena wall: the way out, and the thing that ends the stage.
	///
	/// It sits in the middle of the right-hand wall on every stage, derived rather than
	/// authored, so the player learns where the exit is once instead of hunting for it each
	/// level. It stands visibly shut until <see cref="StageObjective"/> says the stage is
	/// finished with them, then opens with a flashing arrow beside it.
	///
	/// The gate holds no rules. <see cref="StageObjective"/> decides when it may be used and
	/// this asks; all this owns is the hole in the wall and the signal that it is there.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageExit : MonoBehaviour
	{
		[Header("Who reaches it")]
		[SerializeField] private BirdMovement m_Bird;

		[Header("What opens it")]
		[SerializeField] private StageObjective m_Objective;

		[Header("Rendering")]
		[SerializeField] private Sprite m_GateClosed;

		[Tooltip("Must be fully opaque: the cell becomes walkable floor underneath it.")]
		[SerializeField] private Sprite m_GateOpen;

		[Tooltip("Flashes one cell inside the gate once the stage lets the bird leave.")]
		[SerializeField] private Sprite m_Arrow;

		[Tooltip("Arrow flashes per second.")]
		[SerializeField] private float m_ArrowFlashRate = 3f;

		[Tooltip("Sorting order for the gate. Above the arena tiles, below the birds.")]
		[SerializeField] private int m_SortingOrder = 2;

		[Tooltip("Sorting order for the arrow. Above the gate, below the birds.")]
		[SerializeField] private int m_ArrowSortingOrder = 7;

		private BomberBird.Arena.Arena m_Arena;
		private ArenaGrid m_Grid;
		private SpriteRenderer m_Renderer;
		private SpriteRenderer m_ArrowRenderer;
		private Vector2Int m_Cell;
		private bool m_IsCleared;
		private bool m_WasOpen;

		/// <summary>
		/// Whether the gate may be used. An exit with no objective assigned stays usable, so
		/// a stage built without one is playable rather than unfinishable.
		/// </summary>
		public bool IsOpen
		{
			get { return m_Objective == null || m_Objective.IsExitOpen; }
		}

		private void Awake()
		{
			m_Arena = GetComponent<BomberBird.Arena.Arena>();

			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			m_Grid = m_Arena.Grid;
			m_Cell = GateCellFor(m_Grid);

			if (!validatePlacement())
			{
				enabled = false;
				return;
			}

			createVisual();
		}

		/// <summary>
		/// Where the gate goes: the middle of the right-hand wall. Derived from the arena's
		/// own size rather than authored, because a gate that moves between stages is a gate
		/// the player has to find again, and a mis-typed one is an unfinishable stage.
		/// </summary>
		public static Vector2Int GateCellFor(ArenaGrid i_Grid)
		{
			return new Vector2Int(i_Grid.Width - 1, i_Grid.Height / 2);
		}

		private void Update()
		{
			bool isOpen = IsOpen;

			if (isOpen != m_WasOpen)
			{
				m_WasOpen = isOpen;

				if (isOpen)
				{
					openGate();
				}
			}

			if (m_IsCleared || !isOpen || m_Bird.Cell != m_Cell)
			{
				return;
			}

			m_IsCleared = true;

			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow in the scene, so the stage cannot be cleared.", this);
				return;
			}

			GameFlow.Instance.CompleteStage();
		}

		/// <summary>
		/// Opens the wall so the bird can walk into it, shows the open gate, and starts the
		/// arrow. The grid change is what actually lets the bird through; the rest is signal.
		/// </summary>
		private void openGate()
		{
			m_Grid.TryOpen(m_Cell);

			if (m_GateOpen != null)
			{
				m_Renderer.sprite = m_GateOpen;
			}

			if (m_Arrow != null)
			{
				StartCoroutine(flashArrow());
			}
		}

		private IEnumerator flashArrow()
		{
			GameObject visual = new GameObject("ExitArrow");
			visual.transform.SetParent(transform, false);

			// One cell inside the gate, on the floor the bird has to cross to reach it.
			visual.transform.localPosition = m_Grid.CellToWorld(m_Cell + Vector2Int.left);

			m_ArrowRenderer = visual.AddComponent<SpriteRenderer>();
			m_ArrowRenderer.sprite = m_Arrow;
			m_ArrowRenderer.sortingOrder = m_ArrowSortingOrder;

			float secondsPerFlash = m_ArrowFlashRate <= 0f ? 0.25f : 1f / m_ArrowFlashRate;

			// Runs until the stage ends, which is what the player is being told to do.
			while (true)
			{
				m_ArrowRenderer.enabled = !m_ArrowRenderer.enabled;

				yield return new WaitForSeconds(secondsPerFlash);
			}
		}

		private void createVisual()
		{
			GameObject visual = new GameObject("Gate");
			visual.transform.SetParent(transform, false);
			visual.transform.localPosition = m_Grid.CellToWorld(m_Cell);

			m_Renderer = visual.AddComponent<SpriteRenderer>();
			m_Renderer.sprite = m_GateClosed;
			m_Renderer.sortingOrder = m_SortingOrder;

			// Deliberately not asking whether the stage is already clear. This runs in Awake,
			// and the mynas may not have spawned yet, so "nothing left alive" would be read
			// as "stage finished" and the gate would be drawn open before the level began.
			// Starting shut and letting the first Update correct it costs one frame and
			// cannot be wrong.
			m_WasOpen = false;
		}

		/// <summary>
		/// The gate replaces a piece of wall, and the bird has to be able to walk up to it.
		/// Both are level-design mistakes rather than runtime conditions, so they are worth
		/// saying loudly the moment the stage loads.
		/// </summary>
		private bool validatePlacement()
		{
			if (m_Grid.GetCell(m_Cell) != eCell.Border)
			{
				Debug.LogError(
					name + ": the gate at " + m_Cell + " is not border wall. The arena's right-hand "
					+ "wall must be solid border for the gate to sit in.", this);
				return false;
			}

			Vector2Int approach = m_Cell + Vector2Int.left;

			if (!m_Grid.IsWalkable(approach))
			{
				Debug.LogWarning(
					name + ": the cell inside the gate at " + approach + " is not open floor, so the "
					+ "bird may not be able to reach the exit.", this);
			}

			return true;
		}

		private bool hasRequiredReferences()
		{
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

			if (m_GateClosed == null || m_GateOpen == null)
			{
				Debug.LogError(name + ": the gate sprites are not assigned.", this);
				return false;
			}

			return true;
		}
	}
}
