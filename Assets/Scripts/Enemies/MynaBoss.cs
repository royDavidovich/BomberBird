using UnityEngine;

namespace BomberBird.Enemies
{
	/// <summary>
	/// Marks one myna as the boss and gives it the fight: five lives, a spin when it is hit,
	/// and a wave of slaves that grows with every hit it survives.
	///
	/// Carrying this component is what makes a myna the boss - <see cref="ArenaMynas"/> looks
	/// for it rather than keeping a separate list, so a boss is spawned through exactly the
	/// same path as any other myna.
	///
	/// The rules live in <see cref="BossHealth"/>, which is plain C# and tested on its own.
	/// This is the Unity half: the tuning fields and the visible reaction.
	/// </summary>
	[RequireComponent(typeof(MynaMovement))]
	public class MynaBoss : MonoBehaviour
	{
		[Tooltip("Bursts the boss survives. The last one kills it.")]
		[SerializeField] private int m_Lives = 5;

		[Tooltip("Speed added per surviving hit, so the fight quickens as it is won.")]
		[SerializeField] private float m_SpeedStep = 0.25f;

		[Tooltip("Speed the boss never passes, or the bird cannot catch it at the end.")]
		[SerializeField] private float m_MaxSpeed = 3f;

		[Tooltip("Seconds the boss turns on the spot after a hit it survived.")]
		[SerializeField] private float m_SpinSeconds = 0.6f;

		private MynaMovement m_Movement;
		private BossHealth m_Health;

		/// <summary>Lives still in hand, for anything that wants to show the fight's progress.</summary>
		public int LivesRemaining
		{
			get { return m_Health == null ? m_Lives : m_Health.LivesRemaining; }
		}

		/// <summary>
		/// Spends one life, for one burst that caught the boss.
		///
		/// Deliberately called once per explosion rather than once per frame the boss stands
		/// in the flames: <see cref="ArenaMynas"/> drives this from PodExploded, so a single
		/// burst can only ever cost one life however long the fire lingers.
		/// </summary>
		public eBossHit TakeHit(out int o_SlavesSummoned)
		{
			eBossHit outcome = m_Health.TakeHit(out o_SlavesSummoned);

			if (outcome == eBossHit.Survived)
			{
				m_Movement.Speed = m_Health.Speed;
				m_Movement.Spin(m_SpinSeconds);
			}

			return outcome;
		}

		/// <summary>
		/// Slows the whole fight by a scale, for the easy mode.
		///
		/// It rebuilds the ramp rather than only the walking speed, because the ramp is what
		/// the boss climbs back up: a boss slowed at spawn but left with the hard ceiling
		/// would be back at full pace a hit or two later, which is the opposite of what the
		/// mode is for. <see cref="ArenaMynas"/> calls this at spawn, long before the first
		/// burst can land, and <see cref="Awake"/> has already run by then - it is called
		/// during Instantiate, which is why the scaling cannot simply happen before it.
		/// </summary>
		public void SlowTheFight(float i_Scale)
		{
			if (i_Scale <= 0f)
			{
				return;
			}

			m_SpeedStep *= i_Scale;
			m_MaxSpeed *= i_Scale;
			m_Health = new BossHealth(m_Lives, m_Movement.Speed, m_SpeedStep, m_MaxSpeed);
		}

		private void Awake()
		{
			m_Movement = GetComponent<MynaMovement>();

			// The prefab's authored speed is where the fight starts, so the boss's opening
			// pace stays an Inspector decision rather than being duplicated here.
			m_Health = new BossHealth(m_Lives, m_Movement.Speed, m_SpeedStep, m_MaxSpeed);

			if (m_MaxSpeed < m_Movement.Speed)
			{
				Debug.LogWarning(
					name + ": the speed ceiling is below the boss's starting speed, so it will "
					+ "slow down as it is hit rather than speed up.", this);
			}
		}
	}
}
