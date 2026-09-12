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

		/// <summary>The live grid. Null until Awake has run.</summary>
		public ArenaGrid Grid
		{
			get { return m_Grid; }
		}

		private void Awake()
		{
			if (m_Layout == null)
			{
				Debug.LogError(name + ": m_Layout is not assigned.", this);
				enabled = false;
				return;
			}

			m_Grid = m_Layout.CreateGrid();
		}
	}
}
