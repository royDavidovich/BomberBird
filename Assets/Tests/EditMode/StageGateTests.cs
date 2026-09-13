using BomberBird.Flow;
using NUnit.Framework;

namespace BomberBird.Tests
{
	public class StageGateTests
	{
		private const bool k_AwardsFeather = true;
		private const bool k_Collected = true;
		private const bool k_Dropped = true;

		[Test]
		public void TheExitStaysShutWhileAnyMynaLives()
		{
			Assert.IsFalse(StageGate.IsExitOpen(3, !k_AwardsFeather, !k_Collected));
			Assert.IsFalse(StageGate.IsExitOpen(1, !k_AwardsFeather, !k_Collected));
			Assert.IsFalse(StageGate.IsExitOpen(1, k_AwardsFeather, k_Collected),
				"a collected feather does not excuse a myna still walking about");
		}

		/// <summary>The intro and the boss award no feather, so clearing the arena is all of it.</summary>
		[Test]
		public void AStageWithNoFeatherOpensAsSoonAsTheArenaIsClear()
		{
			Assert.IsTrue(StageGate.IsExitOpen(0, !k_AwardsFeather, !k_Collected));
		}

		[Test]
		public void AStageWithAFeatherStaysShutUntilItIsCollected()
		{
			Assert.IsFalse(StageGate.IsExitOpen(0, k_AwardsFeather, !k_Collected),
				"the feather is the point: dropping it must not be enough");
			Assert.IsTrue(StageGate.IsExitOpen(0, k_AwardsFeather, k_Collected));
		}

		[Test]
		public void TheFeatherIsDueOnlyOnceTheLastMynaFalls()
		{
			Assert.IsFalse(StageGate.IsFeatherDue(2, k_AwardsFeather, !k_Dropped));
			Assert.IsFalse(StageGate.IsFeatherDue(1, k_AwardsFeather, !k_Dropped));
			Assert.IsTrue(StageGate.IsFeatherDue(0, k_AwardsFeather, !k_Dropped));
		}

		[Test]
		public void AStageDropsExactlyOneFeather()
		{
			Assert.IsFalse(StageGate.IsFeatherDue(0, k_AwardsFeather, k_Dropped),
				"Update runs every frame, so a dropped feather must stop being due");
		}

		[Test]
		public void NoFeatherIsDueOnAStageThatAwardsNone()
		{
			Assert.IsFalse(StageGate.IsFeatherDue(0, !k_AwardsFeather, !k_Dropped));
		}

		/// <summary>
		/// Nothing counts the mynas down past zero, but a negative must not be read as
		/// "still some left" if anything ever does.
		/// </summary>
		[Test]
		public void TreatsAnImpossibleNegativeCountAsCleared()
		{
			Assert.IsTrue(StageGate.IsExitOpen(-1, !k_AwardsFeather, !k_Collected));
			Assert.IsTrue(StageGate.IsFeatherDue(-1, k_AwardsFeather, !k_Dropped));
		}
	}
}
