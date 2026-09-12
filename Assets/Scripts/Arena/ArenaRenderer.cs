using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// Draws an <see cref="ArenaGrid"/>. Presentation only: it reads the grid and follows
	/// its changes, and removing it would not alter a single gameplay rule.
	/// </summary>
	[RequireComponent(typeof(Arena))]
	public class ArenaRenderer : MonoBehaviour
	{
		[Header("Data")]
		[SerializeField] private ArenaTileSet m_TileSet;

		[Header("Rendering")]
		[Tooltip("Sorting order for arena tiles. Birds, pods, and bursts draw above this.")]
		[SerializeField] private int m_SortingOrder = 0;

		private Arena m_Arena;
		private ArenaGrid m_Grid;
		private SpriteRenderer[,] m_Tiles;

		private void Awake()
		{
			m_Arena = GetComponent<Arena>();

			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			m_Grid = m_Arena.Grid;
			buildTiles();
		}

		private void OnEnable()
		{
			if (m_Grid != null)
			{
				m_Grid.CellChanged += arenaGrid_CellChanged;
			}
		}

		private void OnDisable()
		{
			if (m_Grid != null)
			{
				m_Grid.CellChanged -= arenaGrid_CellChanged;
			}
		}

		private void arenaGrid_CellChanged(Vector2Int i_Cell)
		{
			refreshTile(i_Cell);
		}

		private bool hasRequiredReferences()
		{
			if (m_Arena.Grid == null)
			{
				Debug.LogError(name + ": the Arena component has no grid. Check its layout.", this);
				return false;
			}

			if (m_TileSet == null)
			{
				Debug.LogError(name + ": m_TileSet is not assigned.", this);
				return false;
			}

			string missing = m_TileSet.DescribeMissingSprites();

			if (missing != null)
			{
				Debug.LogError(name + ": tile set '" + m_TileSet.name + "' is missing sprites: " + missing, this);
				return false;
			}

			return true;
		}

		private void buildTiles()
		{
			m_Tiles = new SpriteRenderer[m_Grid.Width, m_Grid.Height];

			for (int y = 0; y < m_Grid.Height; ++y)
			{
				for (int x = 0; x < m_Grid.Width; ++x)
				{
					Vector2Int cell = new Vector2Int(x, y);
					m_Tiles[x, y] = createTile(cell);
					refreshTile(cell);
				}
			}
		}

		private SpriteRenderer createTile(Vector2Int i_Cell)
		{
			GameObject tile = new GameObject(string.Format("Cell_{0}_{1}", i_Cell.x, i_Cell.y));
			tile.transform.SetParent(transform, false);
			tile.transform.localPosition = m_Grid.CellToWorld(i_Cell);

			SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
			renderer.sortingOrder = m_SortingOrder;

			return renderer;
		}

		private void refreshTile(Vector2Int i_Cell)
		{
			SpriteRenderer renderer = m_Tiles[i_Cell.x, i_Cell.y];
			renderer.sprite = m_TileSet.GetSprite(m_Grid.GetCell(i_Cell), i_Cell);
		}
	}
}
