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
			return GetCoveredCells(i_Grid, i_Origin, i_Range, null);
		}

		/// <summary>
		/// The same cells, and also every indestructible cell that stopped an arm, added to
		/// <paramref name="io_Stops"/> when given one. It has to be read from the arena as it
		/// was before the burst: once the soft blocks it covered are destroyed, an arm that one
		/// of them absorbed would look as if it ran on into whatever lay behind.
		/// </summary>
		public static List<Vector2Int> GetCoveredCells(ArenaGrid i_Grid, Vector2Int i_Origin, int i_Range, List<Vector2Int> io_Stops)
		{
			List<Vector2Int> covered = new List<Vector2Int>();

			if (i_Grid == null || !i_Grid.IsInside(i_Origin))
			{
				return covered;
			}

			covered.Add(i_Origin);

			foreach (Vector2Int direction in sr_Directions)
			{
				Vector2Int? stop = walkArm(i_Grid, i_Origin, direction, i_Range, covered);

				if (stop.HasValue && io_Stops != null)
				{
					io_Stops.Add(stop.Value);
				}
			}

			return covered;
		}

		/// <summary>
		/// Walks one arm, adding what it covers to <paramref name="io_Covered"/> when given one,
		/// and returns the indestructible cell that stopped it, or null when the arm ran out of
		/// range, left the grid, or was absorbed by a soft block.
		/// </summary>
		private static Vector2Int? walkArm(
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
					return null;
				}

				eCell contents = i_Grid.GetCell(cell);

				if (contents == eCell.Border || contents == eCell.HardBlock || contents == eCell.Cage)
				{
					// Indestructible: the arm stops short and does not cover this cell. A cage
					// belongs here and not with the soft blocks - nothing the player aims at it
					// opens it, because clearing the arena is what earns the feather inside.
					return cell;
				}

				if (io_Covered != null)
				{
					io_Covered.Add(cell);
				}

				if (contents == eCell.SoftBlock)
				{
					// The block absorbs the burst. It is destroyed, but nothing behind it is.
					return null;
				}
			}

			return null;
		}
	}
}
