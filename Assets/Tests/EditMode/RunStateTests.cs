using BomberBird.Flow;
using NUnit.Framework;

namespace BomberBird.Tests
{
	public class RunStateTests
	{
		private const int k_StartingLives = 3;

		private static RunState makeRun(int i_StartingLives = k_StartingLives)
		{
			return new RunState(i_StartingLives);
		}

		[Test]
		public void StartsWithFullLivesOnTheFirstStage()
		{
			RunState run = makeRun();

			Assert.AreEqual(k_StartingLives, run.Lives);
			Assert.AreEqual(RunState.k_FirstStage, run.StageNumber);
			Assert.IsFalse(run.IsOver);
		}

		[Test]
		public void LosingALifeDoesNotEndTheRunWhileLivesRemain()
		{
			RunState run = makeRun();

			Assert.IsFalse(run.LoseLife(), "two lives are still in hand");
			Assert.AreEqual(2, run.Lives);

			Assert.IsFalse(run.LoseLife(), "one life is still in hand");
			Assert.AreEqual(1, run.Lives);
		}

		[Test]
		public void TheRunEndsOnTheLastLifeAndNotBefore()
		{
			RunState run = makeRun();

			run.LoseLife();
			run.LoseLife();

			Assert.IsTrue(run.LoseLife(), "the third loss spends the last life");
			Assert.AreEqual(0, run.Lives);
			Assert.IsTrue(run.IsOver);
		}

		[Test]
		public void LivesNeverGoNegative()
		{
			// A death animation and a scene reload overlap, so a second report is possible.
			RunState run = makeRun(1);

			Assert.IsTrue(run.LoseLife());
			Assert.IsTrue(run.LoseLife(), "already over, and it stays over");
			Assert.AreEqual(0, run.Lives);
		}

		[Test]
		public void RestartingRestoresFullLivesAndTheFirstStage()
		{
			RunState run = makeRun();

			run.AdvanceStage();
			run.LoseLife();
			run.LoseLife();
			run.LoseLife();

			run.Restart();

			Assert.AreEqual(k_StartingLives, run.Lives);
			Assert.AreEqual(RunState.k_FirstStage, run.StageNumber);
			Assert.IsFalse(run.IsOver);
		}

		[Test]
		public void AdvancingAStageKeepsTheLivesAlreadySpent()
		{
			RunState run = makeRun();

			run.LoseLife();
			run.AdvanceStage();

			Assert.AreEqual(RunState.k_FirstStage + 1, run.StageNumber);
			Assert.AreEqual(2, run.Lives, "a stage is not a fresh start");
		}

		[Test]
		public void AStartingLifeCountBelowOneIsRaisedRatherThanRefused()
		{
			RunState run = makeRun(0);

			Assert.AreEqual(1, run.Lives, "a misconfigured Inspector value must still be playable");
			Assert.IsFalse(run.IsOver);
		}
	}
}
