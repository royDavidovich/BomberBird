using BomberBird.Flow;
using BomberBird.Player;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	public class RunStateTests
	{
		private const int k_StartingLives = 3;

		/// <summary>A throwaway profile. Only its identity matters to the roster.</summary>
		private static BirdProfile makeBird(string i_Name)
		{
			BirdProfile bird = ScriptableObject.CreateInstance<BirdProfile>();
			bird.name = i_Name;

			return bird;
		}

		private static RunState makeRun(int i_StartingLives = k_StartingLives)
		{
			return new RunState(i_StartingLives, makeBird("starter"));
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

		[Test]
		public void StartsCarryingOnlyTheStarterBird()
		{
			BirdProfile starter = makeBird("hoopoe");
			RunState run = new RunState(k_StartingLives, starter);

			Assert.AreEqual(1, run.Roster.Count);
			Assert.AreSame(starter, run.Roster[0]);
			Assert.AreSame(starter, run.SelectedBird, "the starter is what the player flies until they pick otherwise");
		}

		[Test]
		public void AnUnlockedBirdJoinsTheRoster()
		{
			RunState run = makeRun();
			BirdProfile kingfisher = makeBird("kingfisher");

			Assert.IsTrue(run.UnlockBird(kingfisher));
			Assert.AreEqual(2, run.Roster.Count);
			Assert.Contains(kingfisher, (System.Collections.ICollection)run.Roster);
		}

		/// <summary>
		/// A stage can be replayed after a death, so the same feather can be collected twice.
		/// The roster must not grow a duplicate for it.
		/// </summary>
		[Test]
		public void UnlockingTheSameBirdTwiceChangesNothing()
		{
			RunState run = makeRun();
			BirdProfile kingfisher = makeBird("kingfisher");

			Assert.IsTrue(run.UnlockBird(kingfisher));
			Assert.IsFalse(run.UnlockBird(kingfisher), "the second collection is not a new unlock");
			Assert.AreEqual(2, run.Roster.Count);
		}

		[Test]
		public void RefusesToUnlockNothing()
		{
			RunState run = makeRun();

			Assert.IsFalse(run.UnlockBird(null));
			Assert.AreEqual(1, run.Roster.Count);
		}

		[Test]
		public void OnlyAnEarnedBirdCanBeSelected()
		{
			RunState run = makeRun();
			BirdProfile earned = makeBird("pelican");
			BirdProfile neverEarned = makeBird("chukar");

			run.UnlockBird(earned);

			Assert.IsTrue(run.SelectBird(earned));
			Assert.AreSame(earned, run.SelectedBird);

			Assert.IsFalse(run.SelectBird(neverEarned), "a bird the campaign never gave the player");
			Assert.AreSame(earned, run.SelectedBird, "a refused selection leaves the choice alone");
		}

		/// <summary>
		/// Spending the last life starts the campaign over, and a roster that survived that
		/// would make every feather after it meaningless.
		/// </summary>
		[Test]
		public void RestartingTheRunTakesTheEarnedBirdsBack()
		{
			BirdProfile starter = makeBird("hoopoe");
			BirdProfile kingfisher = makeBird("kingfisher");
			RunState run = new RunState(k_StartingLives, starter);

			run.UnlockBird(kingfisher);
			run.SelectBird(kingfisher);
			run.Restart();

			Assert.AreEqual(1, run.Roster.Count);
			Assert.AreSame(starter, run.Roster[0]);
			Assert.AreSame(starter, run.SelectedBird, "and the player is back on the starter");
		}
	}
}
