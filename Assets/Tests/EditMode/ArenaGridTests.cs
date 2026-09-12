using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Arena.Tests
{
	public class ArenaGridTests
	{
		// Row 0 is the top row, so cell (0, 0) is the BOTTOM-left corner.
		private const string k_Rows =
			"#############\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...ssss....#\n" +
			"#.H.HsH.H.H.#\n" +
			"#....ss.....#\n" +
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
		public void Dimensions_MatchTheAuthoredMap()
		{
			ArenaGrid grid = makeGrid();

			Assert.AreEqual(13, grid.Width);
			Assert.AreEqual(11, grid.Height);
		}

		[Test]
		public void RowZeroIsTheTopRow_SoYIsFlippedOnParse()
		{
			ArenaGrid grid = makeGrid();

			// Authored row 3 ("#...ssss....#") must land at y = 11 - 1 - 3 = 7.
			Assert.AreEqual(eCell.SoftBlock, grid.GetCell(new Vector2Int(4, 7)));

			// Authored row 5 ("#....ss.....#") must land at y = 5.
			Assert.AreEqual(eCell.SoftBlock, grid.GetCell(new Vector2Int(5, 5)));

			// The row above the bottom border is open floor, not a lattice row.
			Assert.AreEqual(eCell.Floor, grid.GetCell(new Vector2Int(1, 1)));
		}

		[Test]
		public void BorderRingsEveryEdge()
		{
			ArenaGrid grid = makeGrid();

			for (int x = 0; x < grid.Width; ++x)
			{
				Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(x, 0)), "bottom edge x=" + x);
				Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(x, grid.Height - 1)), "top edge x=" + x);
			}

			for (int y = 0; y < grid.Height; ++y)
			{
				Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(0, y)), "left edge y=" + y);
				Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(grid.Width - 1, y)), "right edge y=" + y);
			}
		}

		[Test]
		public void HardBlockLattice_SitsOnEvenInteriorCoordinates()
		{
			ArenaGrid grid = makeGrid();
			int found = 0;

			for (int y = 1; y < grid.Height - 1; ++y)
			{
				for (int x = 1; x < grid.Width - 1; ++x)
				{
					if (grid.GetCell(new Vector2Int(x, y)) != eCell.HardBlock)
					{
						continue;
					}

					Assert.AreEqual(0, x % 2, "hard block at odd x=" + x);
					Assert.AreEqual(0, y % 2, "hard block at odd y=" + y);
					++found;
				}
			}

			Assert.AreEqual(20, found, "expected a 5 x 4 lattice");
		}

		[Test]
		public void CellPredicates_AgreeWithCellType()
		{
			ArenaGrid grid = makeGrid();

			Vector2Int floor = new Vector2Int(1, 1);
			Vector2Int border = new Vector2Int(0, 0);
			Vector2Int hard = new Vector2Int(2, 2);
			Vector2Int soft = new Vector2Int(5, 5);

			Assert.IsTrue(grid.IsWalkable(floor));
			Assert.IsFalse(grid.IsBlocking(floor));
			Assert.IsFalse(grid.IsDestructible(floor));

			Assert.IsFalse(grid.IsWalkable(border));
			Assert.IsTrue(grid.IsBlocking(border));
			Assert.IsFalse(grid.IsDestructible(border));

			Assert.IsFalse(grid.IsWalkable(hard));
			Assert.IsTrue(grid.IsBlocking(hard));
			Assert.IsFalse(grid.IsDestructible(hard));

			Assert.IsFalse(grid.IsWalkable(soft));
			Assert.IsTrue(grid.IsBlocking(soft), "a soft block must stop a burst");
			Assert.IsTrue(grid.IsDestructible(soft));
		}

		[Test]
		public void TryDestroy_TurnsSoftBlockIntoFloorOnce()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int soft = new Vector2Int(5, 5);

			Assert.IsTrue(grid.TryDestroy(soft), "first destroy should report a change");
			Assert.AreEqual(eCell.Floor, grid.GetCell(soft));
			Assert.IsFalse(grid.TryDestroy(soft), "destroying floor should report no change");
		}

		[Test]
		public void TryDestroy_RefusesIndestructibleCells()
		{
			ArenaGrid grid = makeGrid();

			Assert.IsFalse(grid.TryDestroy(new Vector2Int(0, 0)), "border");
			Assert.IsFalse(grid.TryDestroy(new Vector2Int(2, 2)), "hard block");
			Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(0, 0)));
			Assert.AreEqual(eCell.HardBlock, grid.GetCell(new Vector2Int(2, 2)));
		}

		[Test]
		public void CellChanged_FiresOnceWithTheDestroyedCell()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int soft = new Vector2Int(5, 5);
			List<Vector2Int> raised = new List<Vector2Int>();

			grid.CellChanged += raised.Add;
			grid.TryDestroy(soft);
			grid.TryDestroy(soft);

			Assert.AreEqual(1, raised.Count, "only an actual change should raise the event");
			Assert.AreEqual(soft, raised[0]);
		}

		[Test]
		public void OutOfBoundsQueries_AreSafeAndBlocking()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int outside = new Vector2Int(-1, 4);
			Vector2Int alsoOutside = new Vector2Int(13, 11);

			Assert.IsFalse(grid.IsInside(outside));
			Assert.IsFalse(grid.IsInside(alsoOutside));

			// A burst running off the map is normal, so these must not throw.
			Assert.IsTrue(grid.IsBlocking(outside), "outside the map stops a burst");
			Assert.IsFalse(grid.IsWalkable(outside));
			Assert.IsFalse(grid.IsDestructible(outside));
			Assert.IsFalse(grid.TryDestroy(outside));
		}

		[Test]
		public void WorldPosition_CentresTheArenaOnTheOrigin()
		{
			ArenaGrid grid = makeGrid();

			Assert.AreEqual(new Vector3(0f, 0f, 0f), grid.CellToWorld(new Vector2Int(6, 5)));
			Assert.AreEqual(new Vector3(-6f, -5f, 0f), grid.CellToWorld(new Vector2Int(0, 0)));
			Assert.AreEqual(new Vector3(6f, 5f, 0f), grid.CellToWorld(new Vector2Int(12, 10)));
		}

		[Test]
		public void RaggedMap_FailsLoudlyWithTheOffendingRow()
		{
			string ragged = "####\n#..#\n###";

			ArgumentException error = Assert.Throws<ArgumentException>(() => new ArenaGrid(ragged));
			StringAssert.Contains("row 2", error.Message);
		}

		[Test]
		public void UnknownCharacter_FailsLoudlyWithItsPosition()
		{
			string bad =
				"#####\n" +
				"#.?.#\n" +
				"#####";

			ArgumentException error = Assert.Throws<ArgumentException>(() => new ArenaGrid(bad));
			StringAssert.Contains("row 1", error.Message);
			StringAssert.Contains("column 2", error.Message);
		}
	}
}
