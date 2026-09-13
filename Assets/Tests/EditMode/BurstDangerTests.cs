using System.Collections.Generic;
using BomberBird.Pods;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class BurstDangerTests
	{
		private const float k_Lethal = 0.45f;

		private static readonly Vector2Int sr_Cell = new Vector2Int(3, 4);

		private static IList<Vector2Int> cells(params Vector2Int[] i_Cells)
		{
			return i_Cells;
		}

		[Test]
		public void AnUnmarkedCellIsNeverBurning()
		{
			BurstDanger danger = new BurstDanger();

			Assert.IsFalse(danger.IsBurning(sr_Cell, 0f));
		}

		[Test]
		public void BurnsEveryCellTheBurstCovered()
		{
			BurstDanger danger = new BurstDanger();
			Vector2Int arm = new Vector2Int(4, 4);

			danger.Mark(cells(sr_Cell, arm), 10f, k_Lethal);

			Assert.IsTrue(danger.IsBurning(sr_Cell, 10f));
			Assert.IsTrue(danger.IsBurning(arm, 10f));
			Assert.IsFalse(danger.IsBurning(new Vector2Int(9, 9), 10f));
		}

		[Test]
		public void StopsBurningOnceTheWindowHasPassed()
		{
			BurstDanger danger = new BurstDanger();

			danger.Mark(cells(sr_Cell), 10f, k_Lethal);

			Assert.IsTrue(danger.IsBurning(sr_Cell, 10f + (k_Lethal * 0.5f)), "still drawn, still lethal");
			Assert.IsFalse(danger.IsBurning(sr_Cell, 10f + k_Lethal), "the window closes on its expiry");
			Assert.IsFalse(danger.IsBurning(sr_Cell, 100f));
		}

		[Test]
		public void ASecondBurstExtendsTheWindow()
		{
			BurstDanger danger = new BurstDanger();

			danger.Mark(cells(sr_Cell), 10f, k_Lethal);
			danger.Mark(cells(sr_Cell), 10.2f, k_Lethal);

			Assert.IsTrue(danger.IsBurning(sr_Cell, 10.5f), "the later burst carries the danger past the first");
		}

		/// <summary>
		/// A chain reaction must never make a cell safe sooner than the burst already
		/// covering it would have, however short the second window is.
		/// </summary>
		[Test]
		public void ASecondBurstNeverShortensTheWindow()
		{
			BurstDanger danger = new BurstDanger();

			danger.Mark(cells(sr_Cell), 10f, k_Lethal);
			danger.Mark(cells(sr_Cell), 10.01f, 0.01f);

			Assert.IsTrue(danger.IsBurning(sr_Cell, 10.3f));
		}

		[Test]
		public void ClearForgetsEveryWindow()
		{
			BurstDanger danger = new BurstDanger();

			danger.Mark(cells(sr_Cell), 10f, k_Lethal);
			danger.Clear();

			Assert.IsFalse(danger.IsBurning(sr_Cell, 10f));
		}

		[Test]
		public void MarkingNothingIsHarmless()
		{
			BurstDanger danger = new BurstDanger();

			Assert.DoesNotThrow(() => danger.Mark(null, 10f, k_Lethal));
		}
	}
}
