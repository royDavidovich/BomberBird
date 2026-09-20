using BomberBird.Flow;
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
		/// Whether the screens should be listening at all.
		///
		/// False while the game is fading between screens: the screen being entered is under
		/// black and has not been seen yet, so a key held down through the transition would
		/// answer a prompt the player has not read. Docs/GDD.md section 5 already promises
		/// input is disabled during a stage transition, and this is where that holds for every
		/// screen at once rather than in each of them.
		/// </summary>
		public static bool IsListening()
		{
			return GameFlow.Instance == null || !GameFlow.Instance.IsTransitioning;
		}

		/// <summary>
		/// A key the player meant for the game: not a mouse button, not part of a system
		/// shortcut, and not one landing behind a transition. A click already selects whatever
		/// it landed on, so the mouse must never stand in for a keystroke.
		/// </summary>
		public static bool WasKeyPressed()
		{
			if (!Input.anyKeyDown || IsModifierHeld() || !IsListening())
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
