using BomberBird.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// The arc a summoned helper flies from the boss to its cell: it leaves from the boss,
	/// lands exactly on the cell, and is highest halfway.
	/// </summary>
	public class SummonFlightTests
	{
		private const float k_Height = 1.5f;
		private const float k_Tolerance = 0.0001f;

		private static readonly Vector3 sr_Boss = new Vector3(2f, 1f, 0f);
		private static readonly Vector3 sr_Cell = new Vector3(-4f, 3f, 0f);

		private static Vector3 at(float i_T)
		{
			return ArenaMynas.SummonArcPosition(sr_Boss, sr_Cell, k_Height, i_T);
		}

		[Test]
		public void TheFlightLeavesFromTheBoss()
		{
			Assert.That(Vector3.Distance(sr_Boss, at(0f)), Is.LessThan(k_Tolerance));
		}

		[Test]
		public void TheFlightLandsExactlyOnTheCell()
		{
			// The walk resumes from the cell centre, so a landing off it would show as a snap.
			Assert.That(Vector3.Distance(sr_Cell, at(1f)), Is.LessThan(k_Tolerance));
		}

		[Test]
		public void TheArcPeaksHalfwayAtItsHeight()
		{
			Vector3 midpoint = Vector3.Lerp(sr_Boss, sr_Cell, 0.5f);

			Assert.AreEqual(midpoint.y + k_Height, at(0.5f).y, k_Tolerance);
			Assert.Less(at(0.25f).y - Vector3.Lerp(sr_Boss, sr_Cell, 0.25f).y, k_Height);
		}

		[Test]
		public void TimeOutsideTheFlightIsHeldAtItsEnds()
		{
			// A late frame must not carry the myna past its cell, nor an early one behind the boss.
			Assert.That(Vector3.Distance(sr_Cell, at(1.3f)), Is.LessThan(k_Tolerance));
			Assert.That(Vector3.Distance(sr_Boss, at(-0.2f)), Is.LessThan(k_Tolerance));
		}
	}
}
