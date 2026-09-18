using BomberBird.Flow;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// The first screen. Play starts the run at the intro stage, deliberately not through the
	/// selection screen: the intro teaches the rules with the hoopoe already in hand, and a
	/// choice offered before the player knows what a bird does is not a choice.
	///
	/// The menu opens with nothing chosen, so a player reaching for the mouse is not told what
	/// to press. Unity will not move a selection that does not exist yet, though, so the first
	/// press of a key hands focus to Play and the keyboard takes over from there.
	/// </summary>
	public class MainMenuScreen : MonoBehaviour
	{
		[Header("Keyboard")]
		[Tooltip("Takes focus on the first key pressed. Left unselected until then.")]
		[SerializeField] private GameObject m_FirstSelected;

		private void Update()
		{
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
		/// allowed to hand focus to Play as well.
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

		/// <summary>Wired to Play.</summary>
		public void Play()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no run to start.", this);
				return;
			}

			GameFlow.Instance.StartStage();
		}

		/// <summary>
		/// Wired to Quit. Does nothing in the editor, which is Unity's behaviour rather than
		/// a bug worth working around.
		/// </summary>
		public void Quit()
		{
			Application.Quit();
		}
	}
}
