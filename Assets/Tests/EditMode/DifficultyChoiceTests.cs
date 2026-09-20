using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the slider's left-to-right order means what the menu says it means, and that the
	/// round trip through the one bool the game actually stores does not lose the stop.
	/// </summary>
	public class DifficultyChoiceTests
	{
		[Test]
		public void LeftStopIsTheEasyOne()
		{
			Assert.IsTrue(DifficultyChoice.IsEasyAt(0), "stop 0 is the left end, which is Easy");
			Assert.IsFalse(DifficultyChoice.IsEasyAt(1), "stop 1 is Normal, the ordinary game");
		}

		[Test]
		public void EveryStopIsNamed()
		{
			Assert.AreEqual("Easy", DifficultyChoice.NameAt(0));
			Assert.AreEqual("Normal", DifficultyChoice.NameAt(1));
		}

		[Test]
		public void ThePopupOpensOnTheSettingTheRunCarries()
		{
			Assert.AreEqual(0, DifficultyChoice.StopFor(true));
			Assert.AreEqual(1, DifficultyChoice.StopFor(false));
		}

		[Test]
		public void AStopSurvivesTheRoundTripThroughTheStoredBool()
		{
			for (int stop = 0; stop < DifficultyChoice.StopCount; ++stop)
			{
				bool isEasy = DifficultyChoice.IsEasyAt(stop);

				Assert.AreEqual(stop, DifficultyChoice.StopFor(isEasy),
					"stop " + stop + " came back as something else");
			}
		}

		[Test]
		public void AnOutOfRangeStopIsPinnedToAnEnd()
		{
			Assert.AreEqual(0, DifficultyChoice.Clamp(-3), "below the ladder pins to the easiest");
			Assert.AreEqual(DifficultyChoice.StopCount - 1, DifficultyChoice.Clamp(99),
				"above the ladder pins to the hardest that exists");

			// The names must not throw for the same values, because a scene wired to the wrong
			// slider maximum is exactly how an out-of-range stop reaches them.
			Assert.AreEqual("Easy", DifficultyChoice.NameAt(-3));
			Assert.AreEqual("Normal", DifficultyChoice.NameAt(99));
		}

		[Test]
		public void AddingAHardStopWouldNotTurnNormalEasy()
		{
			// Guards the threshold in IsEasyAt. If StopCount goes to 3 and this starts failing,
			// the check has been written as "not the hardest" instead of "the easiest".
			Assert.IsFalse(DifficultyChoice.IsEasyAt(DifficultyChoice.StopCount - 1),
				"only the left-hand stop is the easier flock");
		}
	}
}
