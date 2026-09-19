using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// The clock on the results screen. Only the formatting is worth a test: the counting
	/// itself is two additions driven by scene events.
	/// </summary>
	public class StageCountersTests
	{
		[TestCase(0f, "0:00")]
		[TestCase(7.4f, "0:07")]
		[TestCase(59.9f, "0:59", Description = "floors rather than rounding up to a minute")]
		[TestCase(60f, "1:00")]
		[TestCase(67f, "1:07", Description = "pads the seconds")]
		[TestCase(600f, "10:00", Description = "minutes are not capped at one digit")]
		[TestCase(-3f, "0:00", Description = "a negative cannot read as time")]
		public void FormatsTheClock(float i_Seconds, string i_Expected)
		{
			Assert.AreEqual(i_Expected, StageCounters.FormatTime(i_Seconds));
		}
	}
}
