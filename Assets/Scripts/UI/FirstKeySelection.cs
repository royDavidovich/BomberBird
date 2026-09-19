using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// Holds a screen's buttons unselected until the player reaches for the keyboard, then
	/// hands focus to the first of them.
	///
	/// A screen that selects a button as it opens tells a player reaching for the mouse what
	/// to press, and shows a focus arrow nobody asked for. Unity will not move a selection
	/// that does not exist yet, though, so something has to create the first one: this waits
	/// for a key and does it then.
	///
	/// It started inside <see cref="MainMenuScreen"/> and was pulled out when the results
	/// cards and the pause overlay wanted the same opening.
	/// </summary>
	public class FirstKeySelection : MonoBehaviour
	{
		[Header("Keyboard")]
		[Tooltip("Takes focus on the first key pressed. Left unselected until then. A screen "
			+ "whose first button changes arms this at runtime instead.")]
		[SerializeField] private GameObject m_FirstSelected;

		private bool m_IsFirstFrame;

		/// <summary>
		/// Points this at the button to open on, and clears whatever was selected before, so
		/// the screen arrives bare. For a screen with more than one face - the results card is
		/// cleared or game over - where the first button is not known until it is shown.
		/// </summary>
		public void Arm(GameObject i_FirstSelected)
		{
			m_FirstSelected = i_FirstSelected;

			if (EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		/// <summary>
		/// The key that opened the screen must not also select on it. Escape opens the pause
		/// overlay, and its own press is still down on the frame the overlay wakes up.
		/// </summary>
		private void OnEnable()
		{
			m_IsFirstFrame = true;
		}

		private void Update()
		{
			if (m_IsFirstFrame)
			{
				m_IsFirstFrame = false;
				return;
			}

			if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null)
			{
				return;
			}

			if (m_FirstSelected != null && wasKeyboardPressed())
			{
				EventSystem.current.SetSelectedGameObject(m_FirstSelected);
			}
		}

		/// <summary>
		/// A key rather than a click. <see cref="Input.anyKeyDown"/> counts mouse buttons as
		/// keys, and a click already selects whatever it landed on, so the mouse must not be
		/// allowed to hand focus to the first button as well.
		/// </summary>
		private static bool wasKeyboardPressed()
		{
			if (!Input.anyKeyDown)
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
