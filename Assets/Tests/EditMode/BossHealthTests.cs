using BomberBird.Enemies;
using NUnit.Framework;

namespace BomberBird.Tests
{
	public class BossHealthTests
	{
		private const int k_Lives = 5;
		private const float k_StartingSpeed = 2f;
		private const float k_SpeedStep = 0.25f;
		private const float k_MaxSpeed = 2.75f;

		private static BossHealth makeBoss()
		{
			return new BossHealth(k_Lives, k_StartingSpeed, k_SpeedStep, k_MaxSpeed);
		}

		[Test]
		public void TheBossSurvivesFourHitsAndFallsToTheFifth()
		{
			BossHealth boss = makeBoss();
			int slaves;

			for (int hit = 1; hit <= 4; ++hit)
			{
				Assert.AreEqual(eBossHit.Survived, boss.TakeHit(out slaves),
					"hit " + hit + " of five must not be the last");
			}

			Assert.AreEqual(eBossHit.Killed, boss.TakeHit(out slaves));
			Assert.IsTrue(boss.IsDead);
		}

		/// <summary>
		/// The ramp is the whole escalation: the fight gets worse as it is won. A flat count
		/// would leave the last phase no more dangerous than the first.
		/// </summary>
		[Test]
		public void EachNonFatalHitSummonsOneMoreSlaveThanTheHitBefore()
		{
			BossHealth boss = makeBoss();
			int slaves;

			for (int hit = 1; hit <= 4; ++hit)
			{
				boss.TakeHit(out slaves);
				Assert.AreEqual(hit, slaves, "hit " + hit + " should summon " + hit);
			}
		}

		/// <summary>
		/// The arena has to empty on the killing blow rather than peak there, or the player
		/// wins into a screen full of mynas.
		/// </summary>
		[Test]
		public void TheKillingHitSummonsNobody()
		{
			BossHealth boss = makeBoss();
			int slaves;

			for (int hit = 1; hit <= 4; ++hit)
			{
				boss.TakeHit(out slaves);
			}

			Assert.AreEqual(eBossHit.Killed, boss.TakeHit(out slaves));
			Assert.AreEqual(0, slaves);
		}

		[Test]
		public void TheWholeFightSummonsTenSlaves()
		{
			BossHealth boss = makeBoss();
			int slaves;
			int total = 0;

			while (!boss.IsDead)
			{
				boss.TakeHit(out slaves);
				total += slaves;
			}

			Assert.AreEqual(10, total, "1 + 2 + 3 + 4, and nothing on the killing hit");
		}

		[Test]
		public void SpeedRisesWithEveryNonFatalHit()
		{
			BossHealth boss = makeBoss();
			int slaves;
			float previous = boss.Speed;

			Assert.AreEqual(k_StartingSpeed, boss.Speed, "the boss starts at its authored speed");

			for (int hit = 1; hit <= 3; ++hit)
			{
				boss.TakeHit(out slaves);
				Assert.Greater(boss.Speed, previous, "hit " + hit + " should speed the boss up");
				previous = boss.Speed;
			}
		}

		/// <summary>
		/// Without the ceiling, five hits of acceleration leave a boss the bird cannot catch,
		/// which turns the last phase into a chase rather than a fight.
		/// </summary>
		[Test]
		public void SpeedNeverPassesTheCeiling()
		{
			BossHealth boss = new BossHealth(k_Lives, k_StartingSpeed, 10f, k_MaxSpeed);
			int slaves;

			while (!boss.IsDead)
			{
				boss.TakeHit(out slaves);
				Assert.LessOrEqual(boss.Speed, k_MaxSpeed);
			}
		}

		[Test]
		public void HitsAfterDeathChangeNothing()
		{
			BossHealth boss = makeBoss();
			int slaves;

			while (!boss.IsDead)
			{
				boss.TakeHit(out slaves);
			}

			float speedAtDeath = boss.Speed;

			Assert.AreEqual(eBossHit.Killed, boss.TakeHit(out slaves), "a dead boss stays dead");
			Assert.AreEqual(0, slaves, "a dead boss summons nobody");
			Assert.AreEqual(0, boss.LivesRemaining, "lives must not go negative");
			Assert.AreEqual(speedAtDeath, boss.Speed);
		}

		/// <summary>
		/// A zero in the Inspector is a misconfiguration, not a reason to hand the player a
		/// boss that dies to the first pod. Matches how RunState treats a bad life count.
		/// </summary>
		[Test]
		public void ABossAlwaysHasAtLeastOneLife()
		{
			BossHealth boss = new BossHealth(0, k_StartingSpeed, k_SpeedStep, k_MaxSpeed);
			int slaves;

			Assert.AreEqual(1, boss.LivesRemaining);
			Assert.AreEqual(eBossHit.Killed, boss.TakeHit(out slaves));
		}
	}
}
