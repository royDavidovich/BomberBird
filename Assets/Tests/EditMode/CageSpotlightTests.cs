using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// The curve the cage spotlight closes along: from off the screen, dipping just inside the
	/// cage, and settling exactly on it.
	/// </summary>
	public class CageSpotlightTests
	{
		private const float k_From = 1.3f;
		private const float k_To = 0.08f;
		private const float k_Overshoot = 0.2f;

		[Test]
		public void TheHoleStartsAtTheStartRadius()
		{
			Assert.AreEqual(k_From, CageSpotlight.IrisRadius(0f, k_From, k_To, k_Overshoot), 1e-5f);
		}

		[Test]
		public void TheHoleEndsExactlyOnTheCage()
		{
			Assert.AreEqual(k_To, CageSpotlight.IrisRadius(1f, k_From, k_To, k_Overshoot), 1e-5f);
		}

		/// <summary>
		/// The dip is what makes it read as landing, and it is bounded by the overshoot share, so
		/// it can never close past the cage into darkness.
		/// </summary>
		[Test]
		public void TheHoleDipsInsideTheCageByNoMoreThanTheOvershoot()
		{
			float smallest = float.MaxValue;

			for (int i = 0; i <= 200; ++i)
			{
				float radius = CageSpotlight.IrisRadius(i / 200f, k_From, k_To, k_Overshoot);

				if (radius < smallest)
				{
					smallest = radius;
				}
			}

			Assert.Less(smallest, k_To, "The hole never dipped inside the cage.");
			Assert.GreaterOrEqual(smallest, k_To * (1f - k_Overshoot) - 1e-5f);
		}

		[Test]
		public void ProgressOutsideZeroToOneHoldsTheEnds()
		{
			Assert.AreEqual(k_From, CageSpotlight.IrisRadius(-1f, k_From, k_To, k_Overshoot), 1e-5f);
			Assert.AreEqual(k_To, CageSpotlight.IrisRadius(2f, k_From, k_To, k_Overshoot), 1e-5f);
		}
	}
}
