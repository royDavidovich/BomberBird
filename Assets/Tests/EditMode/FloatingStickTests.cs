using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// How far the touch pad's knob is dragged, as the stick value the bird reads.
	/// </summary>
	public class FloatingStickTests
	{
		private const float k_Range = 90f;

		[Test]
		public void AKnobAtTheCentreIsNoPush()
		{
			Assert.AreEqual(Vector2.zero, FloatingStick.StickValue(Vector2.zero, k_Range));
		}

		[Test]
		public void HalfTheRangeIsHalfAPush()
		{
			Vector2 value = FloatingStick.StickValue(new Vector2(0f, 45f), k_Range);

			Assert.AreEqual(0.5f, value.y, 1e-5f);
			Assert.AreEqual(0f, value.x, 1e-5f);
		}

		[Test]
		public void PastTheRangeIsAFullPushTheSameWay()
		{
			Vector2 value = FloatingStick.StickValue(new Vector2(-300f, 0f), k_Range);

			Assert.AreEqual(-1f, value.x, 1e-5f);
			Assert.AreEqual(1f, value.magnitude, 1e-5f);
		}

		[Test]
		public void NoRangeIsNoPush()
		{
			Assert.AreEqual(Vector2.zero, FloatingStick.StickValue(new Vector2(10f, 10f), 0f));
		}
	}
}
