using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// What counts as the player pressing something, for the screens that wait on it.
	///
	/// "Any key" is wider than it sounds. <see cref="Input.anyKeyDown"/> answers to mouse
	/// buttons, to the modifiers on their own, and to every key inside a system shortcut - so
	/// taking a screenshot with Cmd+Shift+4 was enough to start a stage that was waiting.
	/// </summary>
	public static class UiInput
	{
		/// <summary>
		/// Whether a modifier is being held, which means whatever else is pressed belongs to
		/// the operating system rather than to the game.
		/// </summary>
		public static bool IsModifierHeld()
		{
			return Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
				|| Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
				|| Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)
				|| Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
				|| Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);
		}

		/// <summary>
		/// A key the player meant for the game: not a mouse button, not part of a system
		/// shortcut. A click already selects whatever it landed on, so the mouse must never
		/// stand in for a keystroke.
		/// </summary>
		public static bool WasKeyPressed()
		{
			if (!Input.anyKeyDown || IsModifierHeld())
			{
				return false;
			}

			for (int button = 0; button < 3; ++button)
			{
				if (Input.GetMouseButtonDown(button))
				{
					return false;
				}
			}

			return true;
		}
	}
}
