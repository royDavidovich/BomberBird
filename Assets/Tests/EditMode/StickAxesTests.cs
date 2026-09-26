using BomberBird.Player;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// The on-screen stick of the Web build on phones, turned into the arrow keys it stands in
	/// for. A diagonal comes back as two keys held, which the bird already resolves by keeping
	/// the axis it is travelling on.
	/// </summary>
	public class StickAxesTests
	{
		[Test]
		public void ARestingThumbIsNoPush()
		{
			Assert.AreEqual(Vector2Int.zero, BirdMovement.StickAxes(Vector2.zero));
			Assert.AreEqual(Vector2Int.zero, BirdMovement.StickAxes(new Vector2(0.3f, 0.2f)));
		}

		[Test]
		public void EachStraightPushIsOneKey()
		{
			Assert.AreEqual(Vector2Int.right, BirdMovement.StickAxes(new Vector2(1f, 0f)));
			Assert.AreEqual(Vector2Int.left, BirdMovement.StickAxes(new Vector2(-1f, 0f)));
			Assert.AreEqual(Vector2Int.up, BirdMovement.StickAxes(new Vector2(0f, 1f)));
			Assert.AreEqual(Vector2Int.down, BirdMovement.StickAxes(new Vector2(0f, -1f)));
		}

		[Test]
		public void APushLeaningSlightlyOffStraightIsStillOneKey()
		{
			// 15 degrees off right: well inside the straight sector.
			Vector2 push = new Vector2(Mathf.Cos(15f * Mathf.Deg2Rad), Mathf.Sin(15f * Mathf.Deg2Rad));

			Assert.AreEqual(Vector2Int.right, BirdMovement.StickAxes(push));
		}

		[Test]
		public void ADiagonalIsTwoKeysHeld()
		{
			Assert.AreEqual(new Vector2Int(1, 1), BirdMovement.StickAxes(new Vector2(0.7f, 0.7f)));
			Assert.AreEqual(new Vector2Int(-1, -1), BirdMovement.StickAxes(new Vector2(-0.6f, -0.6f)));
		}

		[Test]
		public void ASmallPushCountsByDirectionNotSize()
		{
			// Just past the dead zone, straight up: the lean decides, not how far the thumb went.
			Assert.AreEqual(Vector2Int.up, BirdMovement.StickAxes(new Vector2(0f, 0.45f)));
		}
	}
}
