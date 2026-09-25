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

		/// <summary>
		/// The selection screen is worth a stop only when the roster holds a real decision.
		/// Until a feather has been collected it holds the starter alone.
		/// </summary>
		[Test]
		public void TheStarterAloneIsNotAChoice()
		{
			RunState run = makeRun();

			Assert.IsFalse(run.HasBirdChoice, "only the starter has been earned");
		}

		[Test]
		public void AnUnlockedBirdMakesItAChoice()
		{
			RunState run = new RunState(k_StartingLives, makeBird("hoopoe"));

			run.UnlockBird(makeBird("kingfisher"));

			Assert.IsTrue(run.HasBirdChoice, "two birds is a decision");
		}

		[Test]
		public void RestartingTheRunTakesTheChoiceBackToo()
		{
			RunState run = new RunState(k_StartingLives, makeBird("hoopoe"));
			run.UnlockBird(makeBird("kingfisher"));

			run.Restart();

			Assert.IsFalse(run.HasBirdChoice, "the earned bird went back with the roster");
		}

		/// <summary>
		/// The reason RestoreLives exists. Running out on stage five must cost the lives and
		/// nothing else - not the stage reached, and not the birds earned getting there.
		/// </summary>
		[Test]
		public void RestoringLivesKeepsTheStageAndTheRoster()
		{
			RunState run = makeRun();
			BirdProfile earned = makeBird("earned");

			run.AdvanceStage();
			run.AdvanceStage();
			run.UnlockBird(earned);
			run.SelectBird(earned);
			run.AddStageTotals(7, 9, 42f);

			while (!run.LoseLife())
			{
			}

			run.RestoreLives();

			Assert.AreEqual(k_StartingLives, run.Lives, "the lives should come back");
			Assert.IsFalse(run.IsOver);
			Assert.AreEqual(3, run.StageNumber, "the stage reached must survive");
			Assert.AreEqual(2, run.Roster.Count, "the earned bird must survive");
			Assert.AreSame(earned, run.SelectedBird, "the chosen bird must survive");
			Assert.AreEqual(7, run.TotalMynasDefeated, "campaign totals must survive");
		}

		/// <summary>
		/// The other half of that: Restart still wipes everything, so the two never get
		/// quietly conflated.
		/// </summary>
		[Test]
		public void RestartStillWipesTheRun()
		{
			RunState run = makeRun();

			run.AdvanceStage();
			run.UnlockBird(makeBird("earned"));
			run.AddStageTotals(7, 9, 42f);

			run.Restart();

			Assert.AreEqual(RunState.k_FirstStage, run.StageNumber);
			Assert.AreEqual(1, run.Roster.Count, "only the starter should be left");
			Assert.AreEqual(0, run.TotalMynasDefeated);
			Assert.AreEqual(0, run.TotalPodsPlaced);
			Assert.AreEqual(0f, run.TotalSeconds);
		}

		[Test]
		public void CampaignTotalsAccumulateAcrossStages()
		{
			RunState run = makeRun();

			run.AddStageTotals(4, 6, 30.5f);
			run.AddStageTotals(3, 2, 19.5f);

			Assert.AreEqual(7, run.TotalMynasDefeated);
			Assert.AreEqual(8, run.TotalPodsPlaced);
			Assert.AreEqual(50f, run.TotalSeconds, 0.001f);
		}

		/// <summary>
		/// The rules panel opens on the way into the intro stage, so every death there, and every
		/// continue after the last life, passes the same spot. Only a new campaign may show it again.
		/// </summary>
		[Test]
		public void TheInstructionsAreShownOnceARunAndAgainAfterARestart()
		{
			RunState run = makeRun();

			Assert.IsTrue(run.MarkInstructionsSeen(), "the first time into the intro stage");

			run.LoseLife();

			Assert.IsFalse(run.MarkInstructionsSeen(), "a death does not show it again");

			run.RestoreLives();

			Assert.IsFalse(run.MarkInstructionsSeen(), "nor does continuing after the last life");

			run.Restart();

			Assert.IsTrue(run.MarkInstructionsSeen(), "a fresh campaign teaches it again");
		}

		[Test]
		public void TheCageRuleIsShownOnceARunAndAgainAfterARestart()
		{
			RunState run = makeRun();

			Assert.IsTrue(run.MarkCageRuleShown(), "the first cage stage of the run");
			Assert.IsFalse(run.MarkCageRuleShown(), "a retry, or the next cage stage");

			run.Restart();

			Assert.IsTrue(run.MarkCageRuleShown(), "a fresh campaign teaches it again");
		}

		[Test]
		public void AChoiceForAReplayIsClaimedOnceAndForgottenByARestart()
		{
			RunState run = makeRun();

			Assert.IsFalse(run.ClaimReplayAfterChoice(), "a choice nobody marked arrives at the stage");

			run.MarkChoosingForReplay();

			Assert.IsTrue(run.ClaimReplayAfterChoice(), "the choice after a retry replays");
			Assert.IsFalse(run.ClaimReplayAfterChoice(), "the next choice, after a clear, arrives");

			run.MarkChoosingForReplay();
			run.Restart();

			Assert.IsFalse(run.ClaimReplayAfterChoice(), "a fresh campaign carries no replay over");
		}

		[Test]
		public void TheCageStrikeIsExplainedOnceARunAndAgainAfterARestart()
		{
			RunState run = makeRun();

			Assert.IsTrue(run.MarkCageStrikeExplained());
			Assert.IsFalse(run.MarkCageStrikeExplained());

			run.Restart();

			Assert.IsTrue(run.MarkCageStrikeExplained());
		}

		[Test]
		public void TheTwoCageLessonsAreClaimedSeparately()
		{
			RunState run = makeRun();

			run.MarkCageRuleShown();

			Assert.IsTrue(run.MarkCageStrikeExplained(), "reading the rule does not use up the strike label");
		}
	}
}
