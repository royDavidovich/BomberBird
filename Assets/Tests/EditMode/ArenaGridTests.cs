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
		public void WorldToCell_IsTheInverseOfCellToWorld()
		{
			ArenaGrid grid = makeGrid();

			for (int y = 0; y < grid.Height; ++y)
			{
				for (int x = 0; x < grid.Width; ++x)
				{
					Vector2Int cell = new Vector2Int(x, y);
					Assert.AreEqual(cell, grid.WorldToCell(grid.CellToWorld(cell)));
				}
			}
		}

		[Test]
		public void WorldToCell_SnapsToTheNearerCell()
		{
			ArenaGrid grid = makeGrid();

			// Cell (6, 5) is centred on the origin, so it owns everything within half a unit.
			Assert.AreEqual(new Vector2Int(6, 5), grid.WorldToCell(new Vector3(0.49f, -0.49f, 0f)));
			Assert.AreEqual(new Vector2Int(7, 5), grid.WorldToCell(new Vector3(0.51f, 0f, 0f)));
			Assert.AreEqual(new Vector2Int(6, 6), grid.WorldToCell(new Vector3(0f, 0.51f, 0f)));
		}

		[Test]
		public void IsAreaWalkable_RejectsABodyOverlappingABlock()
		{
			ArenaGrid grid = makeGrid();
			const float halfExtent = 0.35f;

			// (1, 1) is open floor with a hard block diagonally at (2, 2).
			Vector3 openCentre = grid.CellToWorld(new Vector2Int(1, 1));
			Assert.IsTrue(grid.IsAreaWalkable(openCentre, halfExtent));

			// Nudged far enough toward the block at (2, 1)? That cell is floor, so still fine.
			Assert.IsTrue(grid.IsAreaWalkable(openCentre + new Vector3(0.4f, 0f, 0f), halfExtent));

			// Sitting on the hard block itself must fail.
			Vector3 blockCentre = grid.CellToWorld(new Vector2Int(2, 2));
			Assert.IsFalse(grid.IsAreaWalkable(blockCentre, halfExtent));

			// Straddling the boundary into the block must also fail.
			Assert.IsFalse(grid.IsAreaWalkable(blockCentre + new Vector3(-0.5f, 0f, 0f), halfExtent));
		}

		[Test]
		public void IsAreaWalkable_LetsABodyFitAlongACorridor()
		{
			ArenaGrid grid = makeGrid();
			const float halfExtent = 0.35f;

			// Row y = 1 is open all the way across the interior.
			for (int x = 1; x <= 11; ++x)
			{
				Vector3 centre = grid.CellToWorld(new Vector2Int(x, 1));
				Assert.IsTrue(grid.IsAreaWalkable(centre, halfExtent), "blocked at x=" + x);
			}
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

		private const string k_CagedRows =
			"#######\n" +
			"#..c..#\n" +
			"#.....#\n" +
			"#######";

		[Test]
		public void ParsesTheCageCharacter()
		{
			ArenaGrid grid = new ArenaGrid(k_CagedRows);

			Assert.IsTrue(grid.HasCage);
			Assert.AreEqual(new Vector2Int(3, 2), grid.CageCell);
			Assert.AreEqual(eCell.Cage, grid.GetCell(grid.CageCell));
		}

		[Test]
		public void AMapWithoutACageSaysSo()
		{
			ArenaGrid grid = new ArenaGrid(
				"#####\n" +
				"#...#\n" +
				"#####");

			Assert.IsFalse(grid.HasCage);
		}

		/// <summary>
		/// The cage is the feather's lock: solid, and proof against anything the player can
		/// aim at it. Clearing the arena is the only thing that opens it.
		/// </summary>
		[Test]
		public void ACageBlocksAndCannotBeDestroyed()
		{
			ArenaGrid grid = new ArenaGrid(k_CagedRows);
			Vector2Int cage = grid.CageCell;

			Assert.IsTrue(grid.IsBlocking(cage));
			Assert.IsFalse(grid.IsWalkable(cage));
			Assert.IsFalse(grid.IsDestructible(cage));
			Assert.IsFalse(grid.TryDestroy(cage), "a burst must not open the cage");
			Assert.AreEqual(eCell.Cage, grid.GetCell(cage), "and it is still standing");
		}

		[Test]
		public void OpeningTheCageTurnsItToFloorAndAnnouncesIt()
		{
			ArenaGrid grid = new ArenaGrid(k_CagedRows);
			Vector2Int cage = grid.CageCell;
			List<Vector2Int> changed = new List<Vector2Int>();
			grid.CellChanged += changed.Add;

			Assert.IsTrue(grid.TryOpen(cage));
			Assert.IsTrue(grid.IsWalkable(cage), "the feather is now reachable");
			Assert.AreEqual(1, changed.Count, "the display has to hear about it");
			Assert.AreEqual(cage, changed[0]);
			Assert.IsFalse(grid.TryOpen(cage), "opening an already-open cell changes nothing");
		}

		/// <summary>The gate is a hole opened in the wall, by the same mechanism.</summary>
		[Test]
		public void OpeningTheGatesBorderCellLetsTheBirdThrough()
		{
			ArenaGrid grid = new ArenaGrid(k_CagedRows);
			Vector2Int gate = new Vector2Int(grid.Width - 1, grid.Height / 2);

			Assert.AreEqual(eCell.Border, grid.GetCell(gate));
			Assert.IsFalse(grid.IsWalkable(gate));

			Assert.IsTrue(grid.TryOpen(gate));
			Assert.IsTrue(grid.IsWalkable(gate));
		}

		/// <summary>
		/// Nothing else may be opened. A hard block that could be opened would let the
		/// objective quietly rewrite the arena.
		/// </summary>
		[Test]
		public void NothingElseCanBeOpened()
		{
			ArenaGrid grid = new ArenaGrid(
				"#####\n" +
				"#.Hs#\n" +
				"#####");

			Assert.IsFalse(grid.TryOpen(new Vector2Int(2, 1)), "hard block");
			Assert.IsFalse(grid.TryOpen(new Vector2Int(3, 1)), "soft block - that is TryDestroy's job");
			Assert.IsFalse(grid.TryOpen(new Vector2Int(1, 1)), "already floor");
			Assert.IsFalse(grid.TryOpen(new Vector2Int(99, 99)), "outside the arena");
		}

		/// <summary>A stage awards one feather, so a second cage is an authoring mistake.</summary>
		[Test]
		public void RefusesAMapWithTwoCages()
		{
			Assert.Throws<ArgumentException>(() => new ArenaGrid(
				"#######\n" +
				"#.c.c.#\n" +
				"#######"));
		}
	}
}
