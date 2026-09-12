using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// Owns the arena's state for one stage. Everything that needs to know what is where
	/// asks this, so gameplay never reads the display.
	/// </summary>
	public class Arena : MonoBehaviour
	{
		[SerializeField] private ArenaLayout m_Layout;

		private ArenaGrid m_Grid;
		private bool m_ReportedMissingLayout;

		/// <summary>
		/// The live grid, built on first access.
		///
		/// Building lazily rather than only in Awake keeps this independent of component
		/// and script execution order. A consumer whose Awake happens to run first would
		/// otherwise see null, which depends on the order components were added to the
		/// GameObject and breaks silently when that changes.
		/// </summary>
		public ArenaGrid Grid
		{
			get
			{
				ensureGrid();

				return m_Grid;
			}
		}

		/// <summary>The stage this arena was built from, for anything that needs its data.</summary>
		public ArenaLayout Layout
		{
			get { return m_Layout; }
		}

		private void Awake()
		{
			ensureGrid();
		}

		private void ensureGrid()
		{
			if (m_Grid != null)
			{
				return;
			}

			if (m_Layout == null)
			{
				if (!m_ReportedMissingLayout)
				{
					m_ReportedMissingLayout = true;
					Debug.LogError(name + ": m_Layout is not assigned.", this);
				}

				return;
			}

			m_Grid = m_Layout.CreateGrid();
		}
	}
}
