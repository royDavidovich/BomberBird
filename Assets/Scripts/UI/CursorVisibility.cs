using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Hides the mouse cursor while the arena is running, and shows it whenever it is frozen.
	///
	/// Play is keyboard-only (Docs/GDD.md section 4), so a pointer resting over the arena is
	/// only in the way. Every overlay the mouse can use - pause, the stage-ready prompt, results,
	/// game over - stops the clock, so reading <see cref="Time.timeScale"/> covers them all
	/// without each one having to remember the cursor.
	///
	/// LateUpdate rather than Update, so it reads the clock after those overlays have set it
	/// this frame, whatever order Unity runs their Updates in.
	/// </summary>
	public class CursorVisibility : MonoBehaviour
	{
		private void LateUpdate()
		{
			Cursor.visible = Time.timeScale == 0f;
		}

		private void OnDisable()
		{
			// The menus have no copy of this, so leaving the stage must not take the cursor with it.
			Cursor.visible = true;
		}
	}
}
