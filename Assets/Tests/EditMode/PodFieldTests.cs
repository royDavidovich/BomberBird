using System.Collections.Generic;
using BomberBird.Arena;
using BomberBird.Pods;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class PodFieldTests
	{
		private const float k_Fuse = 2f;
		private const int k_Range = 2;
		private const int k_MaxPods = 1;

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

		private static PodField makeField(ArenaGrid i_Grid, int i_MaxPods = k_MaxPods)
		{
			return new PodField(i_Grid, k_Fuse, k_Range, i_MaxPods);
		}

		[Test]
		public void PlacesOnFloorAndRaisesTheEvent()
		{
			PodField field = makeField(makeGrid());
			List<Vector2Int> placed = new List<Vector2Int>();
			field.PodPlaced += placed.Add;

			Assert.IsTrue(field.TryPlace(new Vector2Int(1, 1)));
			Assert.AreEqual(1, field.ActivePodCount);
			Assert.AreEqual(new Vector2Int(1, 1), placed[0]);
		}

		[Test]
		public void RefusesBlockedCellsAndDuplicates()
		{
			ArenaGrid grid = makeGrid();
			PodField field = makeField(grid, 5);

			Assert.IsFalse(field.TryPlace(new Vector2Int(0, 0)), "border");
			Assert.IsFalse(field.TryPlace(new Vector2Int(2, 2)), "hard block");

			Assert.IsTrue(field.TryPlace(new Vector2Int(1, 1)));
			Assert.IsFalse(field.TryPlace(new Vector2Int(1, 1)), "one pod per cell");
			Assert.AreEqual(1, field.ActivePodCount);
		}

		[Test]
		public void RespectsTheActivePodLimit()
		{
			PodField field = makeField(makeGrid(), 1);

			Assert.IsTrue(field.TryPlace(new Vector2Int(1, 1)));
			Assert.IsFalse(field.TryPlace(new Vector2Int(3, 1)), "the limit is one");
		}

		[Test]
		public void AFreshPodDoesNotBlockUntilTheBirdStepsOff()
		{
			// The classic rule: you can walk off the pod you just placed, but not back on.
			PodField field = makeField(makeGrid());
			Vector2Int cell = new Vector2Int(1, 1);

			field.TryPlace(cell);
			Assert.IsFalse(field.IsBlocking(cell), "the bird is still standing on it");

			field.MarkVacated(cell);
			Assert.IsTrue(field.IsBlocking(cell), "having stepped off, it cannot step back on");
		}

		[Test]
		public void MarkVacated_IsSafeToCallRepeatedlyAndOnEmptyCells()
		{
			PodField field = makeField(makeGrid());

			Assert.DoesNotThrow(() => field.MarkVacated(new Vector2Int(5, 5)));

			field.TryPlace(new Vector2Int(1, 1));
			field.MarkVacated(new Vector2Int(1, 1));
			field.MarkVacated(new Vector2Int(1, 1));

			Assert.IsTrue(field.IsBlocking(new Vector2Int(1, 1)));
		}

		[Test]
		public void DetonatesWhenTheFuseRunsOut()
		{
			PodField field = makeField(makeGrid());
			List<Vector2Int> exploded = new List<Vector2Int>();
			field.PodExploded += (cell, covered) => exploded.Add(cell);

			Vector2Int cell = new Vector2Int(1, 1);
			field.TryPlace(cell);

			field.Tick(k_Fuse - 0.1f);
			Assert.AreEqual(0, exploded.Count, "the fuse has not run out yet");
			Assert.AreEqual(1, field.ActivePodCount);

			field.Tick(0.2f);
			Assert.AreEqual(1, exploded.Count);
			Assert.AreEqual(cell, exploded[0]);
			Assert.AreEqual(0, field.ActivePodCount, "a spent pod is gone");
		}

		[Test]
		public void ABurstDestroysSoftBlocksItCovers()
		{
			ArenaGrid grid = new ArenaGrid(
				"#######\n" +
				"#.....#\n" +
				"#...s.#\n" +
				"#.....#\n" +
				"#######");

			PodField field = makeField(grid);
			Vector2Int soft = new Vector2Int(4, 2);

			field.TryPlace(new Vector2Int(3, 2));
			field.Tick(k_Fuse);

			Assert.AreEqual(eCell.Floor, grid.GetCell(soft), "the soft block is cleared");
		}

		[Test]
		public void ABurstSetsOffAnotherPodItReaches()
		{
			ArenaGrid grid = makeGrid();
			PodField field = makeField(grid, 5);
			List<Vector2Int> exploded = new List<Vector2Int>();
			field.PodExploded += (cell, covered) => exploded.Add(cell);

			// Two cells apart on an open row: the first burst reaches the second pod.
			Vector2Int first = new Vector2Int(3, 1);
			Vector2Int second = new Vector2Int(5, 1);

			field.TryPlace(first);
			field.TryPlace(second);

			// Give the first a shorter remaining fuse by ticking before placing? Simpler:
			// detonate it directly and prove the chain.
			field.TryDetonateAt(first);

			Assert.AreEqual(2, exploded.Count, "the second pod should be caught by the chain");
			Assert.AreEqual(first, exploded[0]);
			Assert.AreEqual(second, exploded[1]);
			Assert.AreEqual(0, field.ActivePodCount);
		}

		[Test]
		public void AChainDoesNotDetonateAPodBehindASoftBlock()
		{
			// The soft block absorbs the burst, so the pod behind it survives.
			ArenaGrid grid = new ArenaGrid(
				"#########\n" +
				"#.......#\n" +
				"#..s....#\n" +
				"#.......#\n" +
				"#########");

			PodField field = makeField(grid, 5);
			List<Vector2Int> exploded = new List<Vector2Int>();
			field.PodExploded += (cell, covered) => exploded.Add(cell);

			Vector2Int origin = new Vector2Int(2, 2);
			Vector2Int shielded = new Vector2Int(4, 2);

			Assert.AreEqual(eCell.SoftBlock, grid.GetCell(new Vector2Int(3, 2)));

			field.TryPlace(origin);
			field.TryPlace(shielded);
			field.TryDetonateAt(origin);

			Assert.AreEqual(1, exploded.Count, "only the first pod goes off");
			Assert.AreEqual(1, field.ActivePodCount, "the shielded pod survives");
		}

		[Test]
		public void SimultaneousFuses_EachExplodeExactlyOnce()
		{
			ArenaGrid grid = makeGrid();
			PodField field = makeField(grid, 5);
			List<Vector2Int> exploded = new List<Vector2Int>();
			field.PodExploded += (cell, covered) => exploded.Add(cell);

			// Both placed on the same tick, both within each other's blast.
			field.TryPlace(new Vector2Int(3, 1));
			field.TryPlace(new Vector2Int(5, 1));

			field.Tick(k_Fuse);

			Assert.AreEqual(2, exploded.Count, "each pod explodes once, not twice");
			Assert.AreEqual(0, field.ActivePodCount);
		}

		[Test]
		public void ExplodingClearsTheWayForANewPod()
		{
			PodField field = makeField(makeGrid(), 1);
			Vector2Int cell = new Vector2Int(1, 1);

			field.TryPlace(cell);
			Assert.IsFalse(field.TryPlace(new Vector2Int(3, 1)), "at the limit");

			field.Tick(k_Fuse);

			Assert.IsTrue(field.TryPlace(new Vector2Int(3, 1)), "the limit frees up again");
		}

		[Test]
		public void TheCoveredCellsReportedMatchTheBurstShape()
		{
			ArenaGrid grid = makeGrid();
			PodField field = makeField(grid);
			IList<Vector2Int> reported = null;
			field.PodExploded += (cell, covered) => reported = covered;

			Vector2Int origin = new Vector2Int(5, 5);
			field.TryPlace(origin);
			field.Tick(k_Fuse);

			List<Vector2Int> expected = BurstShape.GetCoveredCells(grid, origin, k_Range);
			CollectionAssert.AreEquivalent(expected, reported);
		}
	}
}
