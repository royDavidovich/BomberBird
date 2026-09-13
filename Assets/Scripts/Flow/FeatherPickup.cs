using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The feather lying on the arena: where it is, and the slow shimmer that says it can
	/// be picked up.
	///
	/// Presentation and position only. Whether collecting it means anything is
	/// <see cref="StageObjective"/>'s business, which is also what builds this.
	/// </summary>
	[RequireComponent(typeof(SpriteRenderer))]
	public class FeatherPickup : MonoBehaviour
	{
		private SpriteRenderer m_Renderer;
		private Sprite[] m_Frames;
		private float m_FrameRate;
		private float m_Timer;
		private int m_Frame;
		private Vector2Int m_Cell;

		/// <summary>The cell the feather lies on.</summary>
		public Vector2Int Cell
		{
			get { return m_Cell; }
		}

		/// <summary>Places the feather and starts it shimmering.</summary>
		public void Initialise(ArenaGrid i_Grid, Vector2Int i_Cell, Sprite[] i_Frames, float i_FrameRate)
		{
			m_Renderer = GetComponent<SpriteRenderer>();
			m_Cell = i_Cell;
			m_Frames = i_Frames;
			m_FrameRate = i_FrameRate;

			transform.position = i_Grid.CellToWorld(i_Cell);

			if (m_Frames != null && m_Frames.Length > 0)
			{
				m_Renderer.sprite = m_Frames[0];
			}
		}

		private void Update()
		{
			if (m_Frames == null || m_Frames.Length < 2 || m_FrameRate <= 0f)
			{
				return;
			}

			m_Timer += Time.deltaTime;

			float secondsPerFrame = 1f / m_FrameRate;

			while (m_Timer >= secondsPerFrame)
			{
				m_Timer -= secondsPerFrame;
				m_Frame = (m_Frame + 1) % m_Frames.Length;
				m_Renderer.sprite = m_Frames[m_Frame];
			}
		}
	}
}
