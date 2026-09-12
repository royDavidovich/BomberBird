using System.Collections.Generic;
using BomberBird.Arena;
using BomberBird.Pods;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class BurstShapeTests
	{
		private const int k_Range = 2;

		// Row 0 is the top row. Hard blocks sit on even interior coordinates.
		private const string k_Rows =
			"#############\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#############";

		private static ArenaGrid makeGrid()
		{
			return new ArenaGrid(k_Rows);
		}

		[Test]
		public void CoversACrossOfTheGivenRange_InOpenSpace()
		{
			ArenaGrid grid = makeGrid();
			// (5, 5) is on an open row; (5, 3) and (5, 7) are open rows too, and the
			// lattice rows above and below have a gap at odd x.
			Vector2Int origin = new Vector2Int(5, 5);

			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, origin, k_Range);

			Assert.Contains(origin, covered, "the pod's own cell is always covered");
			Assert.Contains(new Vector2Int(5, 6), covered);
			Assert.Contains(new Vector2Int(5, 7), covered);
			Assert.Contains(new Vector2Int(5, 4), covered);
			Assert.Contains(new Vector2Int(5, 3), covered);
			Assert.Contains(new Vector2Int(6, 5), covered);
			Assert.Contains(new Vector2Int(7, 5), covered);
			Assert.Contains(new Vector2Int(4, 5), covered);
			Assert.Contains(new Vector2Int(3, 5), covered);

			Assert.AreEqual(1 + 4 * k_Range, covered.Count, "a full cross of range 2 is nine cells");
		}

		[Test]
		public void HardBlockStopsTheArmAndIsNotCovered()
		{
			ArenaGrid grid = makeGrid();
			// From (3, 5), going up hits the lattice row at (3, 6)... which is a gap.
			// Use (2, 5) instead: (2, 6) is a hard block.
			Assert.AreEqual(eCell.HardBlock, grid.GetCell(new Vector2Int(2, 6)));

			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, new Vector2Int(2, 5), k_Range);

			Assert.IsFalse(covered.Contains(new Vector2Int(2, 6)), "a hard block is never covered");
			Assert.IsFalse(covered.Contains(new Vector2Int(2, 7)), "and nothing behind it is reached");
		}

		[Test]
		public void BorderStopsTheArmAndIsNotCovered()
		{
			ArenaGrid grid = makeGrid();

			// (1, 9) is the top-left interior cell; the border is at (0, 9) and (1, 10).
			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, new Vector2Int(1, 9), k_Range);

			Assert.IsFalse(covered.Contains(new Vector2Int(0, 9)), "the border is never covered");
			Assert.IsFalse(covered.Contains(new Vector2Int(1, 10)), "the border is never covered");
			Assert.Contains(new Vector2Int(2, 9), covered, "the open row is still reached");
			Assert.Contains(new Vector2Int(3, 9), covered);
		}

		[Test]
		public void SoftBlockIsCoveredButStopsTheArm()
		{
			// The classic rule: the block is destroyed, nothing behind it is.
			// The origin sits one cell from the block, so the cell BEHIND it is still
			// within range. Without the absorb rule it would be covered.
			ArenaGrid grid = new ArenaGrid(
				"#######\n" +
				"#.....#\n" +
				"#...s.#\n" +
				"#.....#\n" +
				"#######");

			Vector2Int origin = new Vector2Int(3, 2);
			Vector2Int soft = new Vector2Int(4, 2);
			Vector2Int behind = new Vector2Int(5, 2);

			Assert.AreEqual(eCell.SoftBlock, grid.GetCell(soft));
			Assert.AreEqual(eCell.Floor, grid.GetCell(behind), "the cell behind must be open");

			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, origin, k_Range);

			Assert.Contains(soft, covered, "the soft block itself is hit and destroyed");
			Assert.IsFalse(covered.Contains(behind),
				"the block absorbs the burst, even though the cell behind is within range");
		}

		[Test]
		public void RangeIsRespected()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int origin = new Vector2Int(5, 5);

			List<Vector2Int> one = BurstShape.GetCoveredCells(grid, origin, 1);

			Assert.AreEqual(5, one.Count, "range 1 is the centre plus four neighbours");
			Assert.IsFalse(one.Contains(new Vector2Int(7, 5)), "two cells away is out of range");
		}

		[Test]
		public void OriginOutsideTheArena_CoversNothing()
		{
			ArenaGrid grid = makeGrid();

			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, new Vector2Int(-1, 5), k_Range);

			Assert.IsEmpty(covered);
		}

		[Test]
		public void NoCellIsCoveredTwice()
		{
			ArenaGrid grid = makeGrid();

			List<Vector2Int> covered = BurstShape.GetCoveredCells(grid, new Vector2Int(5, 5), k_Range);
			HashSet<Vector2Int> unique = new HashSet<Vector2Int>(covered);

			Assert.AreEqual(covered.Count, unique.Count, "arms must not overlap the centre or each other");
		}
	}
}
