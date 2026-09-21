using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That every waiting prompt breathes to the same shape, and that an Inspector value which
	/// would divide by zero turns the pulse off instead of hiding the prompt behind a NaN.
	/// </summary>
	public class UiPulseTests
	{
		private const float k_Tolerance = 0.0001f;

		[Test]
		public void APulseStartsFullAndReturnsFullEveryPeriod()
		{
			Assert.AreEqual(1f, UiPulse.Alpha(0f, 1.4f, 0.3f), k_Tolerance, "the moment it appears");
			Assert.AreEqual(1f, UiPulse.Alpha(1.4f, 1.4f, 0.3f), k_Tolerance, "one full fade later");
			Assert.AreEqual(1f, UiPulse.Alpha(4.2f, 1.4f, 0.3f), k_Tolerance, "three fades later");
		}

		[Test]
		public void TheDimmestPointIsTheFloorAndItIsHalfwayThrough()
		{
			Assert.AreEqual(0.3f, UiPulse.Alpha(0.7f, 1.4f, 0.3f), k_Tolerance);
			Assert.AreEqual(0.3f, UiPulse.Alpha(2.1f, 1.4f, 0.3f), k_Tolerance, "and every period after");
		}

		[Test]
		public void ThePulseNeverLeavesTheFloorToFullRange()
		{
			// Sampled across two periods rather than at the turning points, because a sign slip
			// in the wave would still pass the two tests above.
			for (int step = 0; step <= 40; step++)
			{
				float alpha = UiPulse.Alpha(step * 0.07f, 1.4f, 0.3f);

				Assert.GreaterOrEqual(alpha, 0.3f - k_Tolerance, "dimmer than the floor");
				Assert.LessOrEqual(alpha, 1f + k_Tolerance, "brighter than the text is");
			}
		}

		[Test]
		public void APeriodOfZeroHoldsThePromptAtFullRatherThanDividingByIt()
		{
			// The Inspector's way of asking for no pulse. Dividing would hand back a NaN alpha,
			// which TextMeshPro draws as nothing at all.
			Assert.AreEqual(1f, UiPulse.Alpha(3f, 0f, 0.3f), k_Tolerance);
			Assert.AreEqual(1f, UiPulse.Alpha(3f, -1f, 0.3f), k_Tolerance, "and a negative one");
		}

		[Test]
		public void AFloorOfOneDoesNotMoveAtAll()
		{
			Assert.AreEqual(1f, UiPulse.Alpha(0.35f, 1.4f, 1f), k_Tolerance);
			Assert.AreEqual(1f, UiPulse.Alpha(0.7f, 1.4f, 1f), k_Tolerance, "not even at the dimmest point");
		}

		[Test]
		public void TimeBeforeTheStartIsStillOnTheCurve()
		{
			// Each screen counts from the moment its hold expires, and Update can run a frame
			// early on the boundary. A negative reading should mirror a positive one, not throw.
			Assert.AreEqual(UiPulse.Alpha(0.35f, 1.4f, 0.3f), UiPulse.Alpha(-0.35f, 1.4f, 0.3f), k_Tolerance);
		}
	}
}
