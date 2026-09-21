using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That every screen naming a stage gets the same word, and that a number past the campaign
	/// still prints rather than vanishing.
	/// </summary>
	public class StageWordsTests
	{
		[Test]
		public void TheCampaignIsSpelledOut()
		{
			Assert.AreEqual("ONE", StageWords.Spelled(1));
			Assert.AreEqual("TWO", StageWords.Spelled(2), "the numeral a pixel face draws as a Z");
			Assert.AreEqual("SIX", StageWords.Spelled(6), "the boss stage, the last one worded");
		}

		[Test]
		public void ANumberBeyondTheCampaignFallsBackToTheNumeral()
		{
			// A seventh stage would be a campaign change, and losing the line is worse than
			// showing a digit on the one screen that would.
			Assert.AreEqual("7", StageWords.Spelled(7));
			Assert.AreEqual("0", StageWords.Spelled(0));
			Assert.AreEqual("-1", StageWords.Spelled(-1));
		}
	}
}
