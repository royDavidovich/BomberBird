using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// How one thrown feather moves: up with its throw, then down to a slow float, its sideways
	/// speed dying away without ever turning round.
	/// </summary>
	public class FeatherConfettiTests
	{
		private const float k_Gravity = 1400f;
		private const float k_Drag = 1f;
		private const float k_FallSpeed = 170f;

		[Test]
		public void GravityTurnsAThrowIntoAFall()
		{
			Vector2 velocity = new Vector2(0f, 800f);

			for (int i = 0; i < 20; ++i)
			{
				velocity = FeatherConfetti.Fall(velocity, 0.05f, k_Gravity, k_Drag, k_FallSpeed);
			}

			Assert.Less(velocity.y, 0f);
		}

		[Test]
		public void AFeatherNeverFallsFasterThanItFloats()
		{
			Vector2 velocity = Vector2.zero;

			for (int i = 0; i < 100; ++i)
			{
				velocity = FeatherConfetti.Fall(velocity, 0.1f, k_Gravity, k_Drag, k_FallSpeed);

				Assert.GreaterOrEqual(velocity.y, -k_FallSpeed);
			}

			Assert.AreEqual(-k_FallSpeed, velocity.y, 0.001f, "It settles at the float speed.");
		}

		[Test]
		public void AFeatherAlreadyFallingFasterKeepsItsSpeed()
		{
			// Only reachable by lowering the cap in the Inspector mid-flight. The feather is
			// left to fall rather than yanked upwards to the new cap.
			Vector2 next = FeatherConfetti.Fall(new Vector2(0f, -400f), 0.1f, k_Gravity, k_Drag, k_FallSpeed);

			Assert.AreEqual(-400f, next.y, 0.001f);
		}

		[Test]
		public void DragSlowsTheThrowWithoutReversingIt()
		{
			Vector2 velocity = new Vector2(1000f, 0f);
			Vector2 next = FeatherConfetti.Fall(velocity, 0.1f, k_Gravity, k_Drag, k_FallSpeed);

			Assert.Less(next.x, velocity.x);
			Assert.Greater(next.x, 0f);
		}

		[Test]
		public void NoTimeMeansNoChange()
		{
			Vector2 velocity = new Vector2(300f, -100f);

			Assert.AreEqual(velocity, FeatherConfetti.Fall(velocity, 0f, k_Gravity, k_Drag, k_FallSpeed));
		}
	}
}
