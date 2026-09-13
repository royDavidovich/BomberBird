using System;
using BomberBird.Arena;
using BomberBird.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class MynaWalkTests
	{
		/// <summary>The stage grid: open corridors with a lattice of hard blocks.</summary>
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

		private static readonly Vector2Int sr_Up = Vector2Int.up;
		private static readonly Vector2Int sr_Down = Vector2Int.down;
		private static readonly Vector2Int sr_Left = Vector2Int.left;
		private static readonly Vector2Int sr_Right = Vector2Int.right;

		private static ArenaGrid makeGrid()
		{
			return new ArenaGrid(k_Rows);
		}

		private static Vector2Int choose(
			ArenaGrid i_Grid, Vector2Int i_Cell, Vector2Int i_Current, Predicate<Vector2Int> i_AlsoBlocked = null)
		{
			return MynaWalk.ChooseDirection(i_Grid, i_Cell, i_Current, i_AlsoBlocked, new System.Random(1));
		}

		/// <summary>
		/// In a corridor the only move that is not a reversal is straight on, so the myna
		/// keeps going rather than jittering on the spot.
		/// </summary>
		[Test]
		public void CarriesStraightOnThroughACorridor()
		{
			ArenaGrid grid = makeGrid();

			// (1,8) is walled left and right by the border and a hard block.
			Assert.AreEqual(sr_Down, choose(grid, new Vector2Int(1, 8), sr_Down));
			Assert.AreEqual(sr_Up, choose(grid, new Vector2Int(1, 8), sr_Up));
		}

		[Test]
		public void NeverTurnsBackWhileAnythingElseIsOpen()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int junction = new Vector2Int(1, 9);

			// Arriving from the left along the top corridor, with open floor ahead and below.
			for (int seed = 0; seed < 20; ++seed)
			{
				Vector2Int chosen = MynaWalk.ChooseDirection(grid, junction, sr_Up, null, new System.Random(seed));

				Assert.AreNotEqual(sr_Down, chosen, "turned back with other ways open");
				Assert.IsTrue(chosen == sr_Right || chosen == sr_Down || chosen == sr_Up);
			}
		}

		[Test]
		public void ChoosesAmongEveryOpenWayAtAJunction()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int junction = new Vector2Int(1, 9);
			bool sawRight = false;
			bool sawDown = false;

			// Entering from the left, so the ways on are right and down.
			for (int seed = 0; seed < 40; ++seed)
			{
				Vector2Int chosen = MynaWalk.ChooseDirection(grid, junction, sr_Right, null, new System.Random(seed));

				sawRight |= chosen == sr_Right;
				sawDown |= chosen == sr_Down;
			}

			Assert.IsTrue(sawRight && sawDown, "a junction should not always resolve the same way");
		}

		[Test]
		public void TurnsBackOutOfADeadEnd()
		{
			ArenaGrid grid = new ArenaGrid(
				"#####\n" +
				"#.#.#\n" +
				"#.#.#\n" +
				"#...#\n" +
				"#####");

			// (1,3) is the closed top of the left shaft, walked into from below.
			Assert.AreEqual(sr_Down, choose(grid, new Vector2Int(1, 3), sr_Up));
		}

		[Test]
		public void StandsStillWhenWalledInOnEverySide()
		{
			ArenaGrid grid = new ArenaGrid(
				"###\n" +
				"#.#\n" +
				"###");

			Assert.AreEqual(Vector2Int.zero, choose(grid, new Vector2Int(1, 1), sr_Up));
		}

		/// <summary>
		/// A placed pod is not part of the map, so the grid alone would walk a myna
		/// straight through one.
		/// </summary>
		[Test]
		public void TreatsAnExtraBlockerAsAWall()
		{
			ArenaGrid grid = makeGrid();
			Vector2Int corridor = new Vector2Int(1, 8);
			Vector2Int ahead = new Vector2Int(1, 7);

			Assert.AreEqual(sr_Down, choose(grid, corridor, sr_Down), "open with no pod");

			Predicate<Vector2Int> pod = cell => cell == ahead;

			Assert.AreEqual(sr_Up, choose(grid, corridor, sr_Down, pod), "a pod ahead turns it back");
		}

		[Test]
		public void RefusesAMissingGridOrRandom()
		{
			ArenaGrid grid = makeGrid();

			Assert.Throws<ArgumentNullException>(
				() => MynaWalk.ChooseDirection(null, Vector2Int.one, sr_Up, null, new System.Random(1)));
			Assert.Throws<ArgumentNullException>(
				() => MynaWalk.ChooseDirection(grid, Vector2Int.one, sr_Up, null, null));
		}
	}
}
