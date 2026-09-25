using BomberBird.Enemies;
using BomberBird.Player;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// The facings a spin shows as it runs out: one turn walks the four once, as the boss's
	/// hit spin always has, and several turns walk them once each, in the same order.
	/// </summary>
	public class MynaSpinTests
	{
		private const float k_Quarter = 0.1f;

		private static readonly eFacing[] sr_OneTurn = { eFacing.Right, eFacing.Up, eFacing.Left, eFacing.Down };

		/// <summary>The facing halfway through each quarter, from the start of the spin to its end.</summary>
		private static eFacing[] walk(int i_Turns)
		{
			int quarters = 4 * i_Turns;
			eFacing[] seen = new eFacing[quarters];

			for (int i = 0; i < quarters; ++i)
			{
				float remaining = (quarters - i - 0.5f) * k_Quarter;
				seen[i] = MynaMovement.SpinFacing(remaining, k_Quarter, i_Turns);
			}

			return seen;
		}

		[Test]
		public void OneTurnPassesThroughEachFacingOnce()
		{
			CollectionAssert.AreEqual(sr_OneTurn, walk(1));
		}

		[Test]
		public void SeveralTurnsRepeatTheSameOrder()
		{
			eFacing[] seen = walk(5);

			for (int i = 0; i < seen.Length; ++i)
			{
				Assert.AreEqual(sr_OneTurn[i % 4], seen[i], "quarter " + i);
			}
		}

		[Test]
		public void TheFullSpinStillOpensOnTheFirstFacing()
		{
			// The first frame of a spin has all of it left, which counts one quarter past the
			// last. Clamped, it shows the opening facing rather than wrapping to the closing one.
			Assert.AreEqual(sr_OneTurn[0], MynaMovement.SpinFacing(4f * k_Quarter, k_Quarter, 1));
			Assert.AreEqual(sr_OneTurn[0], MynaMovement.SpinFacing(20f * k_Quarter, k_Quarter, 5));
		}

		[Test]
		public void ANoLengthQuarterDoesNotDivideByZero()
		{
			Assert.AreEqual(eFacing.Down, MynaMovement.SpinFacing(1f, 0f, 3));
		}
	}
}
