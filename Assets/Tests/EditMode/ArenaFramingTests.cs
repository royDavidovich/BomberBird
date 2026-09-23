using BomberBird.Arena;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the camera fits every arena whole into the right-hand share of the screen, and
	/// leaves the left-hand rest to the HUD, at every aspect the game is checked at.
	/// </summary>
	public class ArenaFramingTests
	{
		private const float k_Tolerance = 0.001f;
		private const float k_Wide = 16f / 9f;
		private const float k_Laptop = 16f / 10f;
		private const float k_Square = 4f / 3f;
		private const float k_TwoThirds = 2f / 3f;

		[Test]
		public void ARegularStageOnSixteenByNineIsHeldByItsHeight()
		{
			// 13 wide fits the two-thirds box with room to spare, so the 11 rows decide.
			Assert.AreEqual(5.5f, ArenaFraming.OrthographicSize(13, 11, k_Wide, k_TwoThirds), k_Tolerance);
		}

		[Test]
		public void TheBossArenaOnSixteenByNineIsHeldByItsWidth()
		{
			Assert.AreEqual(6.328f, ArenaFraming.OrthographicSize(15, 11, k_Wide, k_TwoThirds), k_Tolerance);
		}

		[Test]
		public void AFourByThreeScreenIsHeldByTheWidth()
		{
			Assert.AreEqual(7.3125f, ArenaFraming.OrthographicSize(13, 11, k_Square, k_TwoThirds), k_Tolerance);
		}

		[Test]
		public void TheArenaFitsWholeInsideItsShareAtEveryCheckedAspect()
		{
			float[] aspects = { k_Wide, k_Laptop, k_Square };
			int[] widths = { 13, 15 };

			foreach (float aspect in aspects)
			{
				foreach (int cols in widths)
				{
					float size = ArenaFraming.OrthographicSize(cols, 11, aspect, k_TwoThirds);
					float x = ArenaFraming.CameraX(size, aspect, k_TwoThirds);
					float halfWidth = size * aspect;
					float boxLeft = x - halfWidth + (1f - k_TwoThirds) * 2f * halfWidth;
					float boxRight = x + halfWidth;
					string where = cols + " wide at " + aspect;

					Assert.LessOrEqual(boxLeft, -cols * 0.5f + k_Tolerance, where + ": arena spills into the HUD");
					Assert.GreaterOrEqual(boxRight, cols * 0.5f - k_Tolerance, where + ": arena runs off the right edge");
					Assert.GreaterOrEqual(size, 5.5f - k_Tolerance, where + ": top or bottom wall cut off");
					Assert.AreEqual(0f, (boxLeft + boxRight) * 0.5f, k_Tolerance, where + ": arena not centred in its box");
				}
			}
		}

		[Test]
		public void AFullShareLeavesTheCameraCentred()
		{
			Assert.AreEqual(0f, ArenaFraming.CameraX(5.5f, k_Wide, 1f), k_Tolerance);
		}
	}
}
