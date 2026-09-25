using System;
using System.Collections.Generic;
using BomberBird.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class SummonCellTests
	{
		private const int k_Clearance = 6;
		private const int k_MaxRadius = 8;

		private static readonly Vector2Int sr_Boss = new Vector2Int(10, 10);

		private static Predicate<Vector2Int> openOnly(params Vector2Int[] i_Cells)
		{
			HashSet<Vector2Int> open = new HashSet<Vector2Int>(i_Cells);

			return cell => open.Contains(cell);
		}

		private static bool choose(Vector2Int i_Bird, Predicate<Vector2Int> i_IsOpen, out Vector2Int o_Cell)
		{
			return ArenaMynas.ChooseSummonCell(sr_Boss, i_Bird, k_Clearance, k_MaxRadius, i_IsOpen, out o_Cell);
		}

		/// <summary>
		/// The bug this guards: the bird has just hit the boss from beside it, and the helper
		/// used to appear on the nearest free cell, which was next to the bird.
		/// </summary>
		[Test]
		public void SkipsANearerCellThatIsTooCloseToTheBird()
		{
			Vector2Int bird = new Vector2Int(9, 10);
			Vector2Int besideTheBird = new Vector2Int(11, 10);
			Vector2Int farEnough = new Vector2Int(16, 10);
			Vector2Int cell;

			bool found = choose(bird, openOnly(besideTheBird, farEnough), out cell);

			Assert.IsTrue(found);
			Assert.AreEqual(farEnough, cell);
		}

		[Test]
		public void AmongCellsFarEnoughTheOneNearestTheBossWins()
		{
			Vector2Int bird = new Vector2Int(4, 10);
			Vector2Int nearTheBoss = new Vector2Int(11, 10);
			Vector2Int fartherFromTheBoss = new Vector2Int(16, 10);
			Vector2Int cell;

			choose(bird, openOnly(nearTheBoss, fartherFromTheBoss), out cell);

			Assert.AreEqual(nearTheBoss, cell, "a helper should still read as the boss's own");
		}

		/// <summary>
		/// A hit must never go unanswered just because the bird is standing close.
		/// </summary>
		[Test]
		public void WhenNothingIsFarEnoughTheCellFarthestFromTheBirdIsUsed()
		{
			Vector2Int bird = sr_Boss;
			Vector2Int farthest = new Vector2Int(10, 13);
			Vector2Int cell;

			bool found = choose(
				bird, openOnly(new Vector2Int(11, 10), new Vector2Int(12, 10), farthest), out cell);

			Assert.IsTrue(found);
			Assert.AreEqual(farthest, cell);
		}

		[Test]
		public void NeverChoosesAClosedCell()
		{
			// The bird is far off, so only whether a cell is open can decide.
			Vector2Int farAway = new Vector2Int(0, 0);
			Vector2Int cell;

			bool found = choose(farAway, candidate => candidate.x < sr_Boss.x, out cell);

			Assert.IsTrue(found);
			Assert.Less(cell.x, sr_Boss.x);
		}

		[Test]
		public void FailsOnlyWhenNoCellIsOpen()
		{
			Vector2Int cell;

			Assert.IsFalse(choose(sr_Boss, candidate => false, out cell));
		}
	}
}
