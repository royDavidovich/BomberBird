using BomberBird.Arena;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// Draws the stage's exit and clears the stage when the bird reaches it.
	///
	/// The gate itself belongs to <see cref="StageObjective"/>, because it is a rule about the
	/// whole arena rather than about this one tile. All the exit does is ask, refuse to be used
	/// while the answer is no, and look different depending on the answer.
	///
	/// The two tints stand in for the authored open and closed exit art, which has not arrived.
	/// Without some visible difference the player has no way to know the stage has let them go.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageExit : MonoBehaviour
	{
		[Header("Who reaches it")]
		[SerializeField] private BirdMovement m_Bird;

		[Header("What opens it")]
		[SerializeField] private StageObjective m_Objective;

		[Header("Rendering")]
		[Tooltip("Placeholder until the authored exit tile arrives.")]
		[SerializeField] private Sprite m_Sprite;

		[Tooltip("While the objective is unfinished. Dimmer, clearly not usable yet.")]
		[SerializeField] private Color m_ClosedTint = new Color(0.45f, 0.42f, 0.35f, 1f);

		[Tooltip("Once the stage lets the bird leave.")]
		[SerializeField] private Color m_OpenTint = Color.white;

		[Tooltip("Sorting order. Above the arena tiles, below the pods and the birds.")]
		[SerializeField] private int m_SortingOrder = 2;

		private BomberBird.Arena.Arena m_Arena;
		private ArenaGrid m_Grid;
		private SpriteRenderer m_Renderer;
		private Vector2Int m_Cell;
		private bool m_IsCleared;
		private bool m_WasOpen;

		/// <summary>
		/// Whether the exit may be used. An exit with no objective assigned stays usable, so
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
			m_Cell = m_Arena.Layout.ExitCell;

			if (!m_Grid.IsWalkable(m_Cell))
			{
				Debug.LogError(
					name + ": the exit at " + m_Cell + " is not open floor. Check the stage layout.", this);
				enabled = false;
				return;
			}

			createVisual();
		}

		private void Update()
		{
			bool isOpen = IsOpen;

			if (isOpen != m_WasOpen)
			{
				m_WasOpen = isOpen;
				m_Renderer.color = isOpen ? m_OpenTint : m_ClosedTint;
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

		private void createVisual()
		{
			GameObject visual = new GameObject("Exit");
			visual.transform.SetParent(transform, false);
			visual.transform.localPosition = m_Grid.CellToWorld(m_Cell);

			m_Renderer = visual.AddComponent<SpriteRenderer>();
			m_Renderer.sprite = m_Sprite;
			m_Renderer.color = IsOpen ? m_OpenTint : m_ClosedTint;
			m_Renderer.sortingOrder = m_SortingOrder;

			m_WasOpen = IsOpen;
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

			if (m_Sprite == null)
			{
				Debug.LogError(name + ": m_Sprite is not assigned.", this);
				return false;
			}

			return true;
		}
	}
}
