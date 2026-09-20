using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.Flow
{
	/// <summary>
	/// The black the game passes through between screens.
	///
	/// It lives on the <see cref="GameFlow"/> object rather than in any scene, because that is
	/// the only object that survives a scene load: a cover authored in the scene being left
	/// would be destroyed by the load it is hiding, and one authored in the scene being entered
	/// cannot exist until after it has already snapped into view.
	///
	/// It decides nothing and times nothing. <see cref="GameFlow"/> owns the transition and
	/// drives this a frame at a time, so the cover, the clock and the music all move off one
	/// number instead of three timers agreeing with each other.
	/// </summary>
	public class ScreenFade : MonoBehaviour
	{
		[Tooltip("The full-screen black. Its canvas sorts above every scene's, so it covers "
			+ "the HUD and the overlays as well as the arena.")]
		[SerializeField] private Image m_Cover;

		/// <summary>
		/// Puts the cover at an opacity, where 0 is the game and 1 is black.
		///
		/// The object is switched off at 0 rather than left at zero alpha, so a transparent
		/// cover cannot swallow a mouse click on the screen underneath it.
		/// </summary>
		public void Cover(float i_Alpha)
		{
			if (m_Cover == null)
			{
				return;
			}

			float alpha = Mathf.Clamp01(i_Alpha);
			bool isShowing = alpha > 0f;

			if (m_Cover.gameObject.activeSelf != isShowing)
			{
				m_Cover.gameObject.SetActive(isShowing);
			}

			Color colour = m_Cover.color;
			colour.a = alpha;
			m_Cover.color = colour;
		}

		/// <summary>
		/// How far through a fade the given elapsed time is, from 0 to 1.
		///
		/// Static and pure so the shape of a transition can be tested without a scene, a
		/// coroutine or a frame. A zero or negative duration is a cut: it reports finished at
		/// once rather than dividing by zero.
		/// </summary>
		public static float Ramp(float i_Elapsed, float i_Seconds)
		{
			if (i_Seconds <= 0f)
			{
				return 1f;
			}

			return Mathf.Clamp01(i_Elapsed / i_Seconds);
		}

		private void Awake()
		{
			// The cover is worked on with it visible, and would otherwise ship black.
			Cover(0f);
		}
	}
}
