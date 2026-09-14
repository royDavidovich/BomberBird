using System.Collections.Generic;
using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// One handmade stage, authored as text so the arena's shape is visible while editing
	/// and a new stage is a data change rather than a scene edit.
	/// </summary>
	[CreateAssetMenu(fileName = "stage", menuName = "BomberBird/Arena Layout")]
	public class ArenaLayout : ScriptableObject
	{
		public const int k_Width = 13;
		public const int k_Height = 11;

		private const string k_DefaultRows =
			"#############\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...scss....#\n" +
			"#.H.HsH.H.H.#\n" +
			"#....ss.....#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#############";

		[Tooltip("Cells the mynas start on. Each must be open floor in the map below, and none "
			+ "may sit on the bird's start cell or it dies the instant the stage loads.")]
		[SerializeField]
		private Vector2Int[] m_MynaSpawnCells =
		{
			new Vector2Int(11, 9),
			new Vector2Int(1, 1),
			new Vector2Int(7, 5),
		};

		[Tooltip("Cells the boss starts on. Leave empty on a stage with no boss, which is "
			+ "every stage but the last. Only the first cell is used.")]
		[SerializeField]
		private Vector2Int[] m_BossSpawnCells = new Vector2Int[0];

		[Header("Arena map")]
		[Tooltip("Row 0 is the TOP row.  '#' border, 'H' hard block, 's' soft block, "
			+ "'c' the feather's cage, '.' floor.  A stage that awards a bird needs exactly one 'c'.")]
		[SerializeField]
		[TextArea(k_Height, k_Height)]
		private string m_Rows = k_DefaultRows;

		public string Rows
		{
			get { return m_Rows; }
		}

		/// <summary>
		/// Where this stage's mynas start. Authored with the map for the same reason the
		/// exit is: moving an enemy should not mean opening a scene.
		/// </summary>
		public IList<Vector2Int> MynaSpawnCells
		{
			get { return m_MynaSpawnCells; }
		}

		/// <summary>
		/// Where this stage's boss starts, or empty on a stage that has none. An array rather
		/// than a flag plus a cell, so "no boss" is the natural default and five of the six
		/// stages say nothing at all.
		/// </summary>
		public IList<Vector2Int> BossSpawnCells
		{
			get { return m_BossSpawnCells; }
		}

		/// <summary>Builds the runtime grid for this stage.</summary>
		public ArenaGrid CreateGrid()
		{
			ArenaGrid grid = new ArenaGrid(m_Rows);

			// Deliberately not a check against k_Width and k_Height. A stage is allowed its
			// own size - the boss stage is wider to give the fight room - and a mistyped map
			// is already caught harder than a warning: ArenaGrid throws on a ragged row.
			//
			// What a stage may not have is an even dimension. The hard-block lattice is laid
			// out as ".H.H.H.", so an even width or height puts blocks against the border and
			// closes the lanes the arena is built from.
			if (grid.Width % 2 == 0 || grid.Height % 2 == 0)
			{
				Debug.LogWarningFormat(this,
					"{0}: arena is {1}x{2}. Both sides should be odd, or the hard-block lattice "
					+ "does not meet the border cleanly.",
					name, grid.Width, grid.Height);
			}

			return grid;
		}
	}
}
