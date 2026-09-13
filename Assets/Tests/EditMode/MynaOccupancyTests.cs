using System;
using System.Collections.Generic;
using BomberBird.Arena;
using BomberBird.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// That a myna holds both ends of a step, and that holding them is enough to turn
	/// another myna around.
	/// </summary>
	public class MynaOccupancyTests
	{
		/// <summary>One long open corridor along the bottom, so a step is unambiguous.</summary>
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

		private static readonly Vector2Int[] sr_Steps =
			{ Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

		private readonly List<GameObject> r_Spawned = new List<GameObject>();

		[TearDown]
		public void TearDown()
		{
			for (int i = 0; i < r_Spawned.Count; ++i)
			{
				UnityEngine.Object.DestroyImmediate(r_Spawned[i]);
			}

			r_Spawned.Clear();
		}

		private MynaMovement makeMyna(ArenaGrid i_Grid, Vector2Int i_Cell)
		{
			GameObject host = new GameObject("Myna under test");
			r_Spawned.Add(host);

			MynaMovement myna = host.AddComponent<MynaMovement>();
			myna.Initialise(i_Grid, i_Cell, null, new System.Random(1));

			return myna;
		}

		/// <summary>The neighbour the myna reserved when it was placed.</summary>
		private static Vector2Int claimedNeighbour(MynaMovement i_Myna, Vector2Int i_Cell)
		{
			Vector2Int claimed = Vector2Int.zero;
			int found = 0;

			for (int i = 0; i < sr_Steps.Length; ++i)
			{
				if (i_Myna.Occupies(i_Cell + sr_Steps[i]))
				{
					claimed = i_Cell + sr_Steps[i];
					++found;
				}
			}

			Assert.AreEqual(1, found, "A myna should reserve exactly one neighbouring cell.");

			return claimed;
		}

		[Test]
		public void AMynaClaimsTheCellItStandsOnAndTheOneItIsWalkingInto()
		{
			ArenaGrid grid = new ArenaGrid(k_Rows);
			Vector2Int start = new Vector2Int(5, 1);
			MynaMovement myna = makeMyna(grid, start);

			Assert.IsTrue(myna.Occupies(start), "A myna should claim the cell it stands on.");
			Assert.IsFalse(myna.Occupies(start + new Vector2Int(2, 0)),
				"A myna should not claim a cell two steps away.");

			claimedNeighbour(myna, start);
		}

		/// <summary>
		/// The bug this guards. Half way through a step the myna's visible cell flips to the
		/// one ahead, and if the claim flipped with it the cell behind would read as free -
		/// which is how two mynas swapping places walk straight through each other.
		/// </summary>
		[Test]
		public void AMynaStillClaimsTheCellItLeftOnceItIsPastTheMiddleOfAStep()
		{
			ArenaGrid grid = new ArenaGrid(k_Rows);
			Vector2Int start = new Vector2Int(5, 1);
			MynaMovement myna = makeMyna(grid, start);
			Vector2Int target = claimedNeighbour(myna, start);

			// Nudge it most of the way across, so the cell it appears to be in is the target.
			myna.transform.position = Vector3.Lerp(
				grid.CellToWorld(start), grid.CellToWorld(target), 0.9f);

			Assert.AreEqual(target, myna.Cell, "Past the middle, the myna should look like it is ahead.");
			Assert.IsTrue(myna.Occupies(target));
			Assert.IsTrue(myna.Occupies(start),
				"A myna part way through a step must still hold the cell behind it, or another "
				+ "myna will walk into the gap and the two will pass through each other.");
		}

		[Test]
		public void AMynaWillNotWalkIntoACellAnotherMynaHasClaimed()
		{
			ArenaGrid grid = new ArenaGrid(k_Rows);
			Vector2Int start = new Vector2Int(5, 1);
			MynaMovement standing = makeMyna(grid, start);

			Predicate<Vector2Int> blocked = standing.Occupies;

			// A second myna one cell to the left, already walking right, straight at it.
			Vector2Int walker = start + Vector2Int.left;
			Vector2Int chosen = MynaWalk.ChooseDirection(
				grid, walker, Vector2Int.right, blocked, new System.Random(1));

			Assert.AreNotEqual(Vector2Int.right, chosen,
				"A myna should not step into a cell another myna is standing on.");
			Assert.IsFalse(blocked(walker + chosen),
				"Whatever it picked instead must itself be free.");
		}

		/// <summary>
		/// Nose to nose in a one-wide corridor there is nowhere to go but back, which is the
		/// turning around that makes a bump readable rather than a stall.
		/// </summary>
		[Test]
		public void TwoMynasMeetingInACorridorTurnAround()
		{
			// A corridor one cell tall: only left and right are open at (5, 1).
			ArenaGrid grid = new ArenaGrid(
				"#############\n" +
				"#.H.H.H.H.H.#\n" +
				"#...........#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#.H.H.H.H.H.#\n" +
				"#############");

			// x = 6 is the only column with a hard block directly above and below it, so a
			// myna there really has nowhere to go but along the corridor.
			Vector2Int walker = new Vector2Int(6, 8);
			Vector2Int ahead = new Vector2Int(7, 8);
			MynaMovement blocker = makeMyna(grid, ahead);

			Assert.IsTrue(grid.IsWalkable(walker) && grid.IsWalkable(ahead));
			Assert.IsFalse(grid.IsWalkable(walker + Vector2Int.up), "The corridor is not one cell tall.");
			Assert.IsFalse(grid.IsWalkable(walker + Vector2Int.down), "The corridor is not one cell tall.");

			Vector2Int chosen = MynaWalk.ChooseDirection(
				grid, walker, Vector2Int.right, blocker.Occupies, new System.Random(1));

			Assert.AreEqual(Vector2Int.left, chosen,
				"Blocked ahead and walled above and below, the myna should reverse.");
		}
	}
}
