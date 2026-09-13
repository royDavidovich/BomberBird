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

		/// <summary>
		/// Replaces the stage this arena builds, for a run that picks the arena rather than
		/// playing the one the scene was authored with.
		///
		/// It refuses once the grid exists rather than rebuilding it, because by then the
		/// mynas, the pods, and the bird are all holding cells from the old map. Swapping
		/// underneath them would leave them standing in walls, so a late call is a bug in the
		/// caller's timing and says so.
		/// </summary>
		public void UseLayout(ArenaLayout i_Layout)
		{
			if (i_Layout == null)
			{
				Debug.LogError(name + ": UseLayout was given no layout.", this);
				return;
			}

			if (m_Grid != null)
			{
				Debug.LogError(
					name + ": the grid is already built, so the layout cannot change now.", this);
				return;
			}

			m_Layout = i_Layout;
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
