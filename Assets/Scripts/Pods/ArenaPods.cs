using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// Gives the seed-pod rules an arena and a clock. It builds the <see cref="PodField"/>
	/// from the Inspector's tuning values and advances its fuses every frame.
	///
	/// The rules themselves live in <see cref="PodField"/>, which needs no scene at all.
	/// This component is only the bridge between them and the running game.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class ArenaPods : MonoBehaviour
	{
		[Header("Tuning")]
		[Tooltip("Seconds between placing a pod and its burst.")]
		[SerializeField] private float m_FuseSeconds = 2f;

		[Tooltip("How many cells a burst reaches along each of the four directions.")]
		[SerializeField] private int m_BurstRange = 2;

		[Tooltip("How many pods may sit on the arena at once.")]
		[SerializeField] private int m_MaxActivePods = 1;

		private BomberBird.Arena.Arena m_Arena;
		private PodField m_Field;
		private bool m_ReportedMissingGrid;

		/// <summary>Seconds a fuse runs for, so a display can pace itself against it.</summary>
		public float FuseSeconds
		{
			get { return m_FuseSeconds; }
		}

		/// <summary>
		/// How many pods the player holds when none are placed. A pod is spent on placement
		/// and returns when it bursts, so what is left is this minus the pods on the arena.
		/// </summary>
		public int MaxActivePods
		{
			get { return m_MaxActivePods; }
		}

		/// <summary>
		/// The live pod rules, built on first access.
		///
		/// Built lazily for the same reason <see cref="BomberBird.Arena.Arena.Grid"/> is:
		/// a consumer whose Awake happens to run first would otherwise see null, which
		/// depends on the order components were added and breaks silently when that changes.
		/// </summary>
		public PodField Field
		{
			get
			{
				ensureField();

				return m_Field;
			}
		}

		private void Awake()
		{
			ensureField();
		}

		private void Update()
		{
			if (m_Field == null)
			{
				return;
			}

			m_Field.Tick(Time.deltaTime);
		}

		private void ensureField()
		{
			if (m_Field != null)
			{
				return;
			}

			if (m_Arena == null)
			{
				m_Arena = GetComponent<BomberBird.Arena.Arena>();
			}

			ArenaGrid grid = m_Arena.Grid;

			if (grid == null)
			{
				if (!m_ReportedMissingGrid)
				{
					m_ReportedMissingGrid = true;
					Debug.LogError(name + ": the Arena component has no grid. Check its layout.", this);
				}

				return;
			}

			m_Field = new PodField(grid, m_FuseSeconds, m_BurstRange, m_MaxActivePods);
		}
	}
}
