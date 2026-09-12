using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// Draws an <see cref="ArenaGrid"/>. Presentation only: it reads the grid and follows
	/// its changes, and removing it would not alter a single gameplay rule.
	/// </summary>
	public class ArenaRenderer : MonoBehaviour
	{
		[Header("Data")]
		[SerializeField] private ArenaLayout m_Layout;
		[SerializeField] private ArenaTileSet m_TileSet;

		[Header("Rendering")]
		[Tooltip("Sorting order for arena tiles. Birds, pods, and bursts draw above this.")]
		[SerializeField] private int m_SortingOrder = 0;

		private ArenaGrid m_Grid;
		private SpriteRenderer[,] m_Tiles;

		/// <summary>The grid being drawn, once it has been built.</summary>
		public ArenaGrid Grid
		{
			get { return m_Grid; }
		}

		private void Awake()
		{
			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			m_Grid = m_Layout.CreateGrid();
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
			if (m_Layout == null)
			{
				Debug.LogError(name + ": m_Layout is not assigned.", this);
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
