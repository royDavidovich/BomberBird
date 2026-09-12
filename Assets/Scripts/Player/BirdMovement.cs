using System;
using BomberBird.Arena;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// Four-direction movement with free positioning: the bird slides smoothly and may
	/// straddle a cell boundary, the way the reference game moves. Blocking comes from
	/// <see cref="ArenaGrid"/>, never from the scene.
	/// </summary>
	[RequireComponent(typeof(Rigidbody2D))]
	public class BirdMovement : MonoBehaviour
	{
		private const string k_HorizontalAxis = "Horizontal";
		private const string k_VerticalAxis = "Vertical";

		[Header("Arena")]
		[SerializeField] private BomberBird.Arena.Arena m_Arena;

		[Tooltip("Optional. Assign it and a pod the bird has stepped off blocks the way back on.")]
		[SerializeField] private ArenaPods m_Pods;

		[Header("Movement")]
		[Tooltip("Cells per second.")]
		[SerializeField] private float m_Speed = 4f;

		[Tooltip("Half the bird's collision square, in cells. Must stay below 0.5.")]
		[SerializeField] private float m_HalfExtent = 0.35f;

		[Tooltip("How far off a corridor's centre line the bird can be and still be nudged into it. "
			+ "Only nudges when that would actually open the path. Set to 0 to disable.")]
		[SerializeField] private float m_CornerAssist = 0.4f;

		private Rigidbody2D m_Body;
		private ArenaGrid m_Grid;
		private Predicate<Vector2Int> m_IsPodBlocking;
		private Vector2Int m_Intent;
		private eFacing m_Facing = eFacing.Down;
		private bool m_IsMoving;

		/// <summary>Which way the bird is facing, for the animator.</summary>
		public eFacing Facing
		{
			get { return m_Facing; }
		}

		/// <summary>True while the bird actually changed position this step.</summary>
		public bool IsMoving
		{
			get { return m_IsMoving; }
		}

		/// <summary>Half the bird's collision square, in cells.</summary>
		public float HalfExtent
		{
			get { return m_HalfExtent; }
		}

		/// <summary>The cell the bird counts as standing on.</summary>
		public Vector2Int Cell
		{
			get { return m_Grid == null ? Vector2Int.zero : m_Grid.WorldToCell(transform.position); }
		}

		private void Awake()
		{
			m_Body = GetComponent<Rigidbody2D>();
			m_Body.bodyType = RigidbodyType2D.Kinematic;
			m_Body.gravityScale = 0f;

			if (m_Arena == null)
			{
				Debug.LogError(name + ": m_Arena is not assigned.", this);
				enabled = false;
				return;
			}

			if (m_HalfExtent >= 0.5f)
			{
				Debug.LogWarning(name + ": m_HalfExtent must stay below 0.5 or the bird cannot fit a corridor.", this);
			}
		}

		private void Start()
		{
			m_Grid = m_Arena.Grid;

			if (m_Grid == null)
			{
				Debug.LogError(name + ": the Arena has no grid.", this);
				enabled = false;
				return;
			}

			// Cached once: converting a method group every frame would allocate on the
			// movement path for no reason.
			if (m_Pods != null && m_Pods.Field != null)
			{
				m_IsPodBlocking = m_Pods.Field.IsBlocking;
			}
		}

		private void Update()
		{
			// Input belongs in Update, never in FixedUpdate.
			m_Intent = readIntent();

			if (m_Intent != Vector2Int.zero)
			{
				m_Facing = toFacing(m_Intent);
			}
		}

		private void FixedUpdate()
		{
			if (m_Grid == null || m_Intent == Vector2Int.zero)
			{
				m_IsMoving = false;
				return;
			}

			Vector2 from = m_Body.position;
			float step = m_Speed * Time.fixedDeltaTime;
			Vector2 to = from + (Vector2)m_Intent * step;

			if (!m_Grid.IsAreaWalkable(to, m_HalfExtent, m_IsPodBlocking))
			{
				Vector2 assisted;
				to = TryCornerAssist(
					m_Grid, from, m_Intent, step, m_HalfExtent, m_CornerAssist, m_IsPodBlocking, out assisted)
					? assisted
					: from;
			}

			m_IsMoving = to != from;

			if (m_IsMoving)
			{
				m_Body.MovePosition(to);
			}
		}

		/// <summary>
		/// One axis at a time, so the bird follows corridors instead of drifting diagonally.
		/// A new direction wins over one already held, which makes turning feel responsive.
		/// </summary>
		private Vector2Int readIntent()
		{
			int horizontal = Mathf.RoundToInt(Input.GetAxisRaw(k_HorizontalAxis));
			int vertical = Mathf.RoundToInt(Input.GetAxisRaw(k_VerticalAxis));

			if (horizontal != 0 && vertical != 0)
			{
				// Both held: keep the axis the bird is already travelling along.
				bool alreadyHorizontal = m_Facing == eFacing.Left || m_Facing == eFacing.Right;
				return alreadyHorizontal ? new Vector2Int(horizontal, 0) : new Vector2Int(0, vertical);
			}

			return new Vector2Int(horizontal, vertical);
		}

		/// <summary>
		/// Blocked head-on, but only because the bird sits off the corridor's centre line.
		/// Slides it toward that line so a near-miss becomes a turn instead of a stop.
		///
		/// Only assists when reaching the centre line would genuinely open the path.
		/// Walking into a solid wall leaves the bird exactly where it is, rather than
		/// dragging it sideways into a wall it still cannot pass.
		///
		/// <paramref name="i_AlsoBlocked"/> carries anything the grid itself does not know
		/// about, such as a pod the bird has already stepped off. Null asks the grid alone.
		///
		/// Set <paramref name="i_MaxAssist"/> to zero to turn the behaviour off entirely.
		/// </summary>
		public static bool TryCornerAssist(
			ArenaGrid i_Grid,
			Vector2 i_From,
			Vector2Int i_Intent,
			float i_Step,
			float i_HalfExtent,
			float i_MaxAssist,
			Predicate<Vector2Int> i_AlsoBlocked,
			out Vector2 o_Next)
		{
			o_Next = i_From;

			if (i_Grid == null || i_MaxAssist <= 0f || i_Intent == Vector2Int.zero)
			{
				return false;
			}

			bool movingHorizontally = i_Intent.x != 0;
			Vector3 centre = i_Grid.CellToWorld(i_Grid.WorldToCell(i_From));
			float offset = movingHorizontally ? centre.y - i_From.y : centre.x - i_From.x;

			if (Mathf.Approximately(offset, 0f) || Mathf.Abs(offset) > i_MaxAssist)
			{
				return false;
			}

			// Would standing on the centre line actually let the bird through? If not, the
			// slide would be pointless sideways drift against a wall.
			Vector2 aligned = movingHorizontally
				? new Vector2(i_From.x, centre.y)
				: new Vector2(centre.x, i_From.y);

			if (!i_Grid.IsAreaWalkable(aligned + (Vector2)i_Intent * i_Step, i_HalfExtent, i_AlsoBlocked))
			{
				return false;
			}

			float slide = Mathf.Sign(offset) * Mathf.Min(i_Step, Mathf.Abs(offset));
			Vector2 nudged = movingHorizontally
				? new Vector2(i_From.x, i_From.y + slide)
				: new Vector2(i_From.x + slide, i_From.y);

			if (!i_Grid.IsAreaWalkable(nudged, i_HalfExtent, i_AlsoBlocked))
			{
				return false;
			}

			o_Next = nudged;

			return true;
		}

		private static eFacing toFacing(Vector2Int i_Intent)
		{
			eFacing facing;

			if (i_Intent.x > 0)
			{
				facing = eFacing.Right;
			}
			else if (i_Intent.x < 0)
			{
				facing = eFacing.Left;
			}
			else if (i_Intent.y > 0)
			{
				facing = eFacing.Up;
			}
			else
			{
				facing = eFacing.Down;
			}

			return facing;
		}
	}
}
