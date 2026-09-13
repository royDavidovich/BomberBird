using System.Collections.Generic;
using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// Works out which cells a seed pod's burst covers. Pure grid logic with no scene and
	/// no timing, so the rules that decide what a burst reaches can be reasoned about and
	/// tested on their own.
	/// </summary>
	public static class BurstShape
	{
		private static readonly Vector2Int[] sr_Directions =
		{
			Vector2Int.up,
			Vector2Int.down,
			Vector2Int.left,
			Vector2Int.right,
		};

		/// <summary>
		/// The cells a burst centred on <paramref name="i_Origin"/> covers, reaching at most
		/// <paramref name="i_Range"/> cells along each of the four directions.
		///
		/// An arm stops at the first cell it cannot pass. A soft block is included and then
		/// stops the arm, so clearing a path takes more than one pod. A hard block, a cage,
		/// or the border stops the arm without being included.
		/// </summary>
		public static List<Vector2Int> GetCoveredCells(ArenaGrid i_Grid, Vector2Int i_Origin, int i_Range)
		{
			List<Vector2Int> covered = new List<Vector2Int>();

			if (i_Grid == null || !i_Grid.IsInside(i_Origin))
			{
				return covered;
			}

			covered.Add(i_Origin);

			foreach (Vector2Int direction in sr_Directions)
			{
				addArm(i_Grid, i_Origin, direction, i_Range, covered);
			}

			return covered;
		}

		private static void addArm(
			ArenaGrid i_Grid,
			Vector2Int i_Origin,
			Vector2Int i_Direction,
			int i_Range,
			List<Vector2Int> io_Covered)
		{
			for (int distance = 1; distance <= i_Range; ++distance)
			{
				Vector2Int cell = i_Origin + i_Direction * distance;

				if (!i_Grid.IsInside(cell))
				{
					break;
				}

				eCell contents = i_Grid.GetCell(cell);

				if (contents == eCell.Border || contents == eCell.HardBlock || contents == eCell.Cage)
				{
					// Indestructible: the arm stops short and does not cover this cell. A cage
					// belongs here and not with the soft blocks - nothing the player aims at it
					// opens it, because clearing the arena is what earns the feather inside.
					break;
				}

				io_Covered.Add(cell);

				if (contents == eCell.SoftBlock)
				{
					// The block absorbs the burst. It is destroyed, but nothing behind it is.
					break;
				}
			}
		}
	}
}
