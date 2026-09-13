using System;
using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// The authoritative state of one arena. Owns every cell and answers every question
	/// asked about them. Needs no GameObject, no scene, and no Play Mode, so movement and
	/// burst rules can be exercised directly instead of by watching the screen.
	/// </summary>
	public class ArenaGrid
	{
		private const char k_FloorChar = '.';
		private const char k_BorderChar = '#';
		private const char k_HardBlockChar = 'H';
		private const char k_SoftBlockChar = 's';
		private const char k_CageChar = 'c';

		private readonly eCell[,] r_Cells;
		private readonly int r_Width;
		private readonly int r_Height;

		private Vector2Int m_CageCell;
		private bool m_HasCage;

		/// <summary>Raised with the cell whose contents actually changed.</summary>
		public event Action<Vector2Int> CellChanged;

		public int Width
		{
			get { return r_Width; }
		}

		public int Height
		{
			get { return r_Height; }
		}

		/// <summary>Whether this stage authored a cage for its feather.</summary>
		public bool HasCage
		{
			get { return m_HasCage; }
		}

		/// <summary>
		/// Where the cage stands, which is where the feather sits. Meaningless unless
		/// <see cref="HasCage"/>, and recorded while parsing so nothing has to sweep the
		/// grid looking for it.
		/// </summary>
		public Vector2Int CageCell
		{
			get { return m_CageCell; }
		}

		/// <summary>
		/// Parses an authored map. Row 0 of the text is the TOP row, because that is how a
		/// person reads a map, so y is flipped here and nowhere else.
		///
		/// '#' border, 'H' hard block, 's' soft block, 'c' cage, '.' floor.
		/// </summary>
		public ArenaGrid(string i_Rows)
		{
			if (string.IsNullOrEmpty(i_Rows))
			{
				throw new ArgumentException("Arena layout is empty.", "i_Rows");
			}

			string[] rows = splitRows(i_Rows);

			r_Height = rows.Length;
			r_Width = rows[0].Length;

			if (r_Width == 0)
			{
				throw new ArgumentException("Arena layout row 0 is empty.", "i_Rows");
			}

			r_Cells = new eCell[r_Width, r_Height];

			for (int row = 0; row < r_Height; ++row)
			{
				if (rows[row].Length != r_Width)
				{
					throw new ArgumentException(string.Format(
						"Arena layout row {0} has {1} characters, expected {2}.",
						row, rows[row].Length, r_Width), "i_Rows");
				}

				int y = r_Height - 1 - row;

				for (int x = 0; x < r_Width; ++x)
				{
					eCell cell = parseCell(rows[row][x], row, x);

					if (cell == eCell.Cage)
					{
						if (m_HasCage)
						{
							throw new ArgumentException(string.Format(
								"Arena layout has a second cage at row {0}, column {1}. A stage "
								+ "awards one feather, so it holds one cage.", row, x), "i_Rows");
						}

						m_HasCage = true;
						m_CageCell = new Vector2Int(x, y);
					}

					r_Cells[x, y] = cell;
				}
			}
		}

		public bool IsInside(Vector2Int i_Cell)
		{
			return i_Cell.x >= 0 && i_Cell.x < r_Width
				&& i_Cell.y >= 0 && i_Cell.y < r_Height;
		}

		public eCell GetCell(Vector2Int i_Cell)
		{
			if (!IsInside(i_Cell))
			{
				throw new ArgumentOutOfRangeException("i_Cell", i_Cell + " is outside the arena.");
			}

			return r_Cells[i_Cell.x, i_Cell.y];
		}

		/// <summary>True when a burst or a moving bird cannot pass through this cell.</summary>
		public bool IsBlocking(Vector2Int i_Cell)
		{
			// Running off the map is normal for a burst, so answer instead of throwing.
			if (!IsInside(i_Cell))
			{
				return true;
			}

			return r_Cells[i_Cell.x, i_Cell.y] != eCell.Floor;
		}

		public bool IsDestructible(Vector2Int i_Cell)
		{
			return IsInside(i_Cell) && r_Cells[i_Cell.x, i_Cell.y] == eCell.SoftBlock;
		}

		public bool IsWalkable(Vector2Int i_Cell)
		{
			return IsInside(i_Cell) && r_Cells[i_Cell.x, i_Cell.y] == eCell.Floor;
		}

		/// <summary>
		/// Clears a soft block. Returns whether anything actually changed, so a caller never
		/// has to re-read the cell to find out.
		/// </summary>
		public bool TryDestroy(Vector2Int i_Cell)
		{
			if (!IsDestructible(i_Cell))
			{
				return false;
			}

			r_Cells[i_Cell.x, i_Cell.y] = eCell.Floor;
			OnCellChanged(i_Cell);

			return true;
		}

		/// <summary>
		/// Opens a cell the objective has unlocked, turning it into floor and announcing the
		/// change so the display follows. Returns whether anything actually changed.
		///
		/// Deliberately narrow: only a cage and the border cell the gate sits in may be
		/// opened this way. A soft block is <see cref="TryDestroy"/>'s business, and nothing
		/// should be able to punch a hole through the rest of the wall.
		/// </summary>
		public bool TryOpen(Vector2Int i_Cell)
		{
			if (!IsInside(i_Cell))
			{
				return false;
			}

			eCell contents = r_Cells[i_Cell.x, i_Cell.y];

			if (contents != eCell.Cage && contents != eCell.Border)
			{
				return false;
			}

			r_Cells[i_Cell.x, i_Cell.y] = eCell.Floor;
			OnCellChanged(i_Cell);

			return true;
		}

		/// <summary>
		/// Centre of a cell in world space. Dimensions are odd, so the middle cell sits
		/// exactly on the origin and the arena is symmetric around the camera.
		/// </summary>
		public Vector3 CellToWorld(Vector2Int i_Cell)
		{
			return new Vector3(
				i_Cell.x - (r_Width - 1) * 0.5f,
				i_Cell.y - (r_Height - 1) * 0.5f,
				0f);
		}

		/// <summary>
		/// The cell a world position falls in. Inverse of <see cref="CellToWorld"/>. A body
		/// standing between two cells counts as being in the nearer one.
		/// </summary>
		public Vector2Int WorldToCell(Vector3 i_World)
		{
			return new Vector2Int(
				Mathf.RoundToInt(i_World.x + (r_Width - 1) * 0.5f),
				Mathf.RoundToInt(i_World.y + (r_Height - 1) * 0.5f));
		}

		/// <summary>
		/// True when an axis-aligned square centred on a world position sits entirely on
		/// walkable cells. Used for smooth movement, where a body can straddle a boundary.
		/// The square must be smaller than one cell, so testing its four corners is enough.
		/// </summary>
		public bool IsAreaWalkable(Vector2 i_Centre, float i_HalfExtent)
		{
			return IsAreaWalkable(i_Centre, i_HalfExtent, null);
		}

		/// <summary>
		/// The same test, with one addition: a caller may name cells the grid knows nothing
		/// about, such as a placed pod. Passing null asks the arena's own contents only.
		///
		/// The grid never learns what the extra blocker is. It asks a question and believes
		/// the answer, which keeps pods and, later, enemies out of the map's own rules.
		/// </summary>
		public bool IsAreaWalkable(Vector2 i_Centre, float i_HalfExtent, Predicate<Vector2Int> i_AlsoBlocked)
		{
			float left = i_Centre.x - i_HalfExtent;
			float right = i_Centre.x + i_HalfExtent;
			float bottom = i_Centre.y - i_HalfExtent;
			float top = i_Centre.y + i_HalfExtent;

			return isCellFree(WorldToCell(new Vector3(left, bottom, 0f)), i_AlsoBlocked)
				&& isCellFree(WorldToCell(new Vector3(right, bottom, 0f)), i_AlsoBlocked)
				&& isCellFree(WorldToCell(new Vector3(left, top, 0f)), i_AlsoBlocked)
				&& isCellFree(WorldToCell(new Vector3(right, top, 0f)), i_AlsoBlocked);
		}

		private bool isCellFree(Vector2Int i_Cell, Predicate<Vector2Int> i_AlsoBlocked)
		{
			return IsWalkable(i_Cell) && (i_AlsoBlocked == null || !i_AlsoBlocked(i_Cell));
		}

		protected virtual void OnCellChanged(Vector2Int i_Cell)
		{
			Action<Vector2Int> handler = CellChanged;

			if (handler != null)
			{
				handler(i_Cell);
			}
		}

		private static string[] splitRows(string i_Rows)
		{
			// Trim so a trailing newline in the Inspector does not become an empty row, and
			// strip \r so a map authored on Windows parses the same on macOS.
			string[] rows = i_Rows.Replace("\r", string.Empty).Trim('\n').Split('\n');

			return rows;
		}

		private static eCell parseCell(char i_Char, int i_Row, int i_Column)
		{
			eCell cell;

			switch (i_Char)
			{
				case k_FloorChar:
					cell = eCell.Floor;
					break;

				case k_BorderChar:
					cell = eCell.Border;
					break;

				case k_HardBlockChar:
					cell = eCell.HardBlock;
					break;

				case k_SoftBlockChar:
					cell = eCell.SoftBlock;
					break;

				case k_CageChar:
					cell = eCell.Cage;
					break;

				default:
					throw new ArgumentException(string.Format(
						"Arena layout has unknown character '{0}' at row {1}, column {2}.",
						i_Char, i_Row, i_Column), "i_Rows");
			}

			return cell;
		}
	}
}
