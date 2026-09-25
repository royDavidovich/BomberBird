using BomberBird.Flow;
using BomberBird.Player;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// Which screen the run goes to next, and above all when the habitat card is shown: on
	/// arriving at a stage, never on another attempt at one.
	/// </summary>
	public class StageRoutingTests
	{
		private const int k_StageCount = 6;

		private static BirdProfile makeBird(string i_Name)
		{
			BirdProfile bird = ScriptableObject.CreateInstance<BirdProfile>();
			bird.name = i_Name;

			return bird;
		}

		private static RunState makeRun()
		{
			return new RunState(3, makeBird("starter"));
		}

		private static RunState makeRunWithAChoice()
		{
			RunState run = makeRun();
			run.UnlockBird(makeBird("earned"));

			return run;
		}

		[Test]
		public void WithOneBirdAClearArrivesThroughTheHabitatCard()
		{
			RunState run = makeRun();
			run.AdvanceStage();

			Assert.AreEqual(eStageRoute.HabitatCard, run.RouteAfterAdvance(k_StageCount));
		}

		[Test]
		public void WithOneBirdAnotherAttemptGoesStraightIntoTheArena()
		{
			Assert.AreEqual(eStageRoute.Arena, makeRun().RouteToAnotherAttempt());
		}

		[Test]
		public void WithAChoiceAClearGoesThroughSelectionAndThenTheHabitatCard()
		{
			RunState run = makeRunWithAChoice();
			run.AdvanceStage();

			Assert.AreEqual(eStageRoute.BirdSelect, run.RouteAfterAdvance(k_StageCount));
			Assert.AreEqual(eStageRoute.HabitatCard, run.RouteAfterChoice(), "arriving shows the card");
		}

		[Test]
		public void WithAChoiceAnotherAttemptGoesThroughSelectionAndSkipsTheHabitatCard()
		{
			RunState run = makeRunWithAChoice();

			Assert.AreEqual(eStageRoute.BirdSelect, run.RouteToAnotherAttempt());
			Assert.AreEqual(eStageRoute.Arena, run.RouteAfterChoice(), "a replay skips the card");
		}

		[Test]
		public void TheClearAfterAnotherAttemptShowsTheHabitatCardAgain()
		{
			// The replay mark must be spent by the choice it was for, or the next stage would be
			// entered without its card.
			RunState run = makeRunWithAChoice();
			run.RouteToAnotherAttempt();
			run.RouteAfterChoice();
			run.AdvanceStage();

			Assert.AreEqual(eStageRoute.BirdSelect, run.RouteAfterAdvance(k_StageCount));
			Assert.AreEqual(eStageRoute.HabitatCard, run.RouteAfterChoice());
		}

		[Test]
		public void ClearingTheLastStageEndsTheCampaign()
		{
			RunState run = makeRunWithAChoice();

			for (int stage = RunState.k_FirstStage; stage < k_StageCount; ++stage)
			{
				run.AdvanceStage();
			}

			Assert.AreNotEqual(eStageRoute.Closing, run.RouteAfterAdvance(k_StageCount), "on the last stage");

			run.AdvanceStage();

			Assert.AreEqual(eStageRoute.Closing, run.RouteAfterAdvance(k_StageCount), "past it");
		}
	}
}
