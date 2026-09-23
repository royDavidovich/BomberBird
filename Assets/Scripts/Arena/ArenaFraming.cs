using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>
	/// Frames the arena whole into the right-hand share of the screen and leaves the rest,
	/// on the left, to the HUD.
	///
	/// The size comes from the arena's own cells rather than a number typed into the camera,
	/// so the wider boss arena and a 4:3 screen both reframe without anyone retuning it. The
	/// whole arena is always in view: a different screen shows more or less of the backdrop
	/// around it, never more or less of the game.
	/// </summary>
	[RequireComponent(typeof(Camera))]
	public class ArenaFraming : MonoBehaviour
	{
		[SerializeField] private Arena m_Arena;

		[Tooltip("How much of the screen's width the arena gets, from the right. The HUD has "
			+ "the rest, so this must agree with the HUD column's right anchor.")]
		[Range(0.1f, 1f)]
		[SerializeField] private float m_ArenaShare = 2f / 3f;

		private Camera m_Camera;
		private int m_FramedWidth;
		private int m_FramedHeight;

		/// <summary>
		/// The half-height that fits a cols x rows arena into the share: whichever of its
		/// height or its width runs out of room first decides.
		/// </summary>
		public static float OrthographicSize(int i_Cols, int i_Rows, float i_Aspect, float i_ArenaShare)
		{
			float byHeight = i_Rows * 0.5f;
			float byWidth = i_Cols / (2f * i_Aspect * i_ArenaShare);

			return Mathf.Max(byHeight, byWidth);
		}

		/// <summary>
		/// How far left the camera sits so the arena, centred on the world origin, lands in
		/// the middle of the right-hand share rather than the middle of the screen.
		/// </summary>
		public static float CameraX(float i_OrthographicSize, float i_Aspect, float i_ArenaShare)
		{
			float screenWidth = 2f * i_OrthographicSize * i_Aspect;

			return -screenWidth * (1f - i_ArenaShare) * 0.5f;
		}

		private void Awake()
		{
			m_Camera = GetComponent<Camera>();

			if (m_Arena == null)
			{
				Debug.LogError(name + ": m_Arena is not assigned, so the camera keeps its authored framing.", this);
				enabled = false;
			}
		}

		/// <summary>
		/// Checked every frame rather than once, because resizing the Game view to check the
		/// other aspects happens with the scene running. Two int compares when nothing moved.
		/// </summary>
		private void LateUpdate()
		{
			if (Screen.width == m_FramedWidth && Screen.height == m_FramedHeight)
			{
				return;
			}

			ArenaGrid grid = m_Arena.Grid;

			if (grid == null)
			{
				return;
			}

			m_FramedWidth = Screen.width;
			m_FramedHeight = Screen.height;

			float size = OrthographicSize(grid.Width, grid.Height, m_Camera.aspect, m_ArenaShare);
			Vector3 position = transform.position;

			position.x = CameraX(size, m_Camera.aspect, m_ArenaShare);
			m_Camera.orthographicSize = size;
			transform.position = position;
		}
	}
}
