using UnityEngine;

namespace BomberBird.Enemies
{
	/// <summary>
	/// The boss myna's fight: the lives it has left, how fast it has become, and how many
	/// slaves each hit calls into the arena.
	///
	/// The ramp is the escalation. Every non-fatal hit summons one more slave than the hit
	/// before it, so the fight gets worse as it is won, and the killing hit summons nobody so
	/// the arena empties on the blow that ends it rather than peaking there.
	///
	/// Speed rises with each non-fatal hit but is clamped, because five hits of unchecked
	/// acceleration leave a boss the bird cannot catch.
	///
	/// Plain C# with no scene and no MonoBehaviour, so a whole fight can be stepped through
	/// in a test instead of played five times.
	/// </summary>
	public class BossHealth
	{
		private readonly float r_SpeedStep;
		private readonly float r_MaxSpeed;

		private int m_LivesRemaining;
		private int m_HitsTaken;
		private float m_Speed;

		/// <summary>Lives still in hand. Zero means the boss is down.</summary>
		public int LivesRemaining
		{
			get { return m_LivesRemaining; }
		}

		public bool IsDead
		{
			get { return m_LivesRemaining <= 0; }
		}

		/// <summary>How fast the boss walks now, after every hit it has taken.</summary>
		public float Speed
		{
			get { return m_Speed; }
		}

		/// <summary>
		/// A boss always has at least one life. A zero or negative setting is a misconfigured
		/// Inspector value rather than a reason to throw, so it is raised to one - the same
		/// way <see cref="BomberBird.Flow.RunState"/> treats a bad starting life count.
		/// </summary>
		public BossHealth(int i_Lives, float i_StartingSpeed, float i_SpeedStep, float i_MaxSpeed)
		{
			m_LivesRemaining = Mathf.Max(1, i_Lives);
			m_Speed = i_StartingSpeed;
			r_SpeedStep = i_SpeedStep;
			r_MaxSpeed = i_MaxSpeed;
		}

		/// <summary>
		/// Spends one life, for one burst that caught the boss.
		///
		/// <paramref name="o_SlavesSummoned"/> is how many ordinary mynas this hit calls in:
		/// the hit's own number while the boss lives, and none on the hit that kills it.
		/// </summary>
		public eBossHit TakeHit(out int o_SlavesSummoned)
		{
			o_SlavesSummoned = 0;

			if (IsDead)
			{
				// A burst that catches a boss already down must not summon anything, and must
				// not push the life count below zero.
				return eBossHit.Killed;
			}

			--m_LivesRemaining;
			++m_HitsTaken;

			if (IsDead)
			{
				return eBossHit.Killed;
			}

			o_SlavesSummoned = m_HitsTaken;
			m_Speed = Mathf.Min(m_Speed + r_SpeedStep, r_MaxSpeed);

			return eBossHit.Survived;
		}
	}
}
