using BomberBird.Arena;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// Draws the stage's exit and clears the stage when the bird reaches it.
	///
	/// <see cref="IsOpen"/> is the gate the design asks for: defeat every myna, collect the
	/// feather on a regular stage, and only then may the exit be used. Until mynas exist there
	/// is nothing to defeat, so it opens immediately and the gate goes in front of it later.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageExit : MonoBehaviour
	{
		[Header("Who reaches it")]
		[SerializeField] private BirdMovement m_Bird;

		[Header("Rendering")]
		[Tooltip("Placeholder until the authored exit tile arrives.")]
		[SerializeField] private Sprite m_Sprite;

		[SerializeField] private Color m_Tint = Color.white;

		[Tooltip("Sorting order. Above the arena tiles, below the pods and the birds.")]
		[SerializeField] private int m_SortingOrder = 2;

		private BomberBird.Arena.Arena m_Arena;
		private ArenaGrid m_Grid;
		private Vector2Int m_Cell;
		private bool m_IsCleared;

		/// <summary>
		/// Whether the exit may be used. Always true for now; the myna-and-feather objective
		/// will drive it once there are mynas to defeat.
		/// </summary>
		public bool IsOpen
		{
			get { return true; }
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
			if (m_IsCleared || !IsOpen || m_Bird.Cell != m_Cell)
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

			SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
			renderer.sprite = m_Sprite;
			renderer.color = m_Tint;
			renderer.sortingOrder = m_SortingOrder;
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
