using BomberBird.Enemies;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Counts what the player did this stage, for the results screen to report.
	///
	/// It observes and nothing else, the same way <see cref="StageHud"/> and
	/// <see cref="StageAudio"/> do. Deleting this component costs the numbers on one screen
	/// and changes no rule of the game.
	/// </summary>
	public class StageCounters : MonoBehaviour
	{
		[Header("What it reads")]
		[SerializeField] private ArenaPods m_Pods;
		[SerializeField] private ArenaMynas m_Mynas;

		private PodField m_Field;
		private int m_PodsPlaced;
		private float m_Seconds;

		/// <summary>
		/// Mynas defeated, worked out from the roll rather than counted as they fall.
		///
		/// Deliberately not a tally of <see cref="ArenaMynas.MynaDefeated"/>: killing the boss
		/// scatters every surviving myna out of the living set without raising that event, so
		/// an event-driven count would miss the whole final wave of the boss stage.
		/// </summary>
		public int MynasDefeated
		{
			get { return m_Mynas == null ? 0 : m_Mynas.SpawnedCount - m_Mynas.LivingCount; }
		}

		public int PodsPlaced
		{
			get { return m_PodsPlaced; }
		}

		public float Seconds
		{
			get { return m_Seconds; }
		}

		/// <summary>
		/// Minutes and seconds, seconds floored. Static and side-effect free so the one piece
		/// of real logic here can be tested without a scene.
		/// </summary>
		public static string FormatTime(float i_Seconds)
		{
			if (i_Seconds < 0f)
			{
				i_Seconds = 0f;
			}

			int whole = Mathf.FloorToInt(i_Seconds);

			return (whole / 60) + ":" + (whole % 60).ToString("00");
		}

		private void Awake()
		{
			if (m_Pods != null)
			{
				m_Field = m_Pods.Field;
			}
		}

		private void OnEnable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced += podField_PodPlaced;
			}
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced -= podField_PodPlaced;
			}
		}

		/// <summary>
		/// Scaled time on purpose. Time.deltaTime is zero while the game is frozen, so the
		/// clock stops of its own accord for the pause overlay and again for the results
		/// screen, with no start and stop calls to keep in step. The ceiling: this measures
		/// game time, so a future slow-motion effect would pull it away from wall clock.
		/// </summary>
		private void Update()
		{
			m_Seconds += Time.deltaTime;
		}

		private void podField_PodPlaced(Vector2Int i_Cell)
		{
			++m_PodsPlaced;
		}
	}
}
