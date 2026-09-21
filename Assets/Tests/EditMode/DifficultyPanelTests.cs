using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the panel does not read the press that opened it as the press putting it away.
	///
	/// The Difficulty button is reached with the arrows and taken with Return or Space, and both
	/// are close keys. The key stays down for the rest of that frame, so without this guard the
	/// panel opened and shut in the same breath and the button looked dead.
	/// </summary>
	public class DifficultyPanelTests
	{
		[Test]
		public void TheOpeningPressIsNotAlsoTheClosingPress()
		{
			Assert.IsTrue(DifficultyPanel.IsOpeningFrame(120, 120),
				"the panel opened on this frame, so a close key down now is the press that opened it");
		}

		[Test]
		public void TheNextFrameBelongsToThePlayer()
		{
			Assert.IsFalse(DifficultyPanel.IsOpeningFrame(120, 121),
				"a frame later the player has had a chance to press something of their own");
		}

		[Test]
		public void APanelThatWasNeverOpenedHoldsNoFrame()
		{
			// -1 is what the field carries before the first Show, and frame counts start at 0.
			Assert.IsFalse(DifficultyPanel.IsOpeningFrame(-1, 0),
				"nothing opened, so no frame is spoken for");
		}
	}
}
