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
	/// It waits for the keyboard to be empty first. These screens arrive on top of a player
	/// whose hands are already on the keys - the arrow that walked into the gate, the Escape
	/// that opened the pause overlay - and a press that was meant for the arena must not be
	/// read as a choice on the card that interrupted it. So the count starts once nothing is
	/// held, and the first press after that is the player's answer to this screen.
	///
	/// It started inside <see cref="MainMenuScreen"/> and was pulled out when the results
	/// cards and the pause overlay wanted the same opening.
	///
	/// It also gives focus back after a click on empty space, which clears the selection and
	/// would otherwise leave the arrows and Enter doing nothing. The player gets back the
	/// button they were last on, not the first one, so a stray click does not undo their place.
	///
	/// The key that gives focus does only that, because this runs after the EventSystem (ugui
	/// pins it at execution order -1000). Run first, the input module would see the same press
	/// on the button just selected: an arrow would move one past it, and Enter would press a
	/// button the player never saw focused.
	/// </summary>
	public class FirstKeySelection : MonoBehaviour
	{
		[Header("Keyboard")]
		[Tooltip("Takes focus on the first key pressed. Left unselected until then. A screen "
			+ "whose first button changes arms this at runtime instead.")]
		[SerializeField] private GameObject m_FirstSelected;

		[Tooltip("A panel that owns the keyboard while it is up. Optional - only the main menu "
			+ "has one. The same standing-down PauseController and StageReady already do for "
			+ "the rules panel.")]
		[SerializeField] private DifficultyPanel m_Difficulty;

		private bool m_HasSeenIdle;
		private GameObject m_LastSelected;

		/// <summary>
		/// Points this at the button to open on, and clears whatever was selected before, so
		/// the screen arrives bare. For a screen with more than one face - the results card is
		/// cleared or game over - where the first button is not known until it is shown.
		/// </summary>
		public void Arm(GameObject i_FirstSelected)
		{
			m_FirstSelected = i_FirstSelected;
			m_LastSelected = null;

			if (EventSystem.current != null)
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		private void OnEnable()
		{
			m_HasSeenIdle = false;
			m_LastSelected = null;
		}

		/// <summary>
		/// What a keypress on an empty selection brings back: the button last on, while it is
		/// still showing, otherwise the screen's first.
		/// </summary>
		public static GameObject ChooseRestore(GameObject i_Last, GameObject i_First)
		{
			return i_Last != null && i_Last.activeInHierarchy ? i_Last : i_First;
		}

		private void Update()
		{
			// Before the early returns below, so a click made while they hold still leaves
			// something to come back to.
			if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
			{
				m_LastSelected = EventSystem.current.currentSelectedGameObject;
			}

			if (!m_HasSeenIdle)
			{
				// Whatever was being held when this screen opened has to be let go of first.
				m_HasSeenIdle = !Input.anyKey;
				return;
			}

			if (m_Difficulty != null && m_Difficulty.IsShown)
			{
				// The panel is up and reading the keyboard itself. Without this, the keystroke
				// that closes it would land here as "the first key" and hand focus to Play,
				// which is the same one-frame trap the rules panel already guards against.
				return;
			}

			if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject != null)
			{
				return;
			}

			GameObject restore = ChooseRestore(m_LastSelected, m_FirstSelected);

			if (restore != null && UiInput.WasKeyPressed())
			{
				EventSystem.current.SetSelectedGameObject(restore);
			}
		}
	}
}
