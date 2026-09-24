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
				walkArm(i_Grid, i_Origin, direction, i_Range, covered);
			}

			return covered;
		}

		/// <summary>
		/// Whether a burst from <paramref name="i_Origin"/> is stopped by
		/// <paramref name="i_Cell"/>: an arm reaches it within range and it is one of the
		/// cells a burst cannot pass. Lets the cage answer a pod aimed at it, so a player
		/// trying to break it out hears that it holds.
		/// </summary>
		public static bool StopsAt(ArenaGrid i_Grid, Vector2Int i_Origin, int i_Range, Vector2Int i_Cell)
		{
			if (i_Grid == null || !i_Grid.IsInside(i_Origin))
			{
				return false;
			}

			foreach (Vector2Int direction in sr_Directions)
			{
				Vector2Int? stop = walkArm(i_Grid, i_Origin, direction, i_Range, null);

				if (stop.HasValue && stop.Value == i_Cell)
				{
					return true;
				}
			}

			return false;
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
