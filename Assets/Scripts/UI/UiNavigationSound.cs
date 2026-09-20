using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// A click as the keyboard moves between a screen's buttons.
	///
	/// Every screen but one was silent while the focus arrow walked up and down it.
	/// <see cref="BirdSelectScreen"/> was the exception, because it drives its own navigation and
	/// plays the clip from there; every other screen leaves the moving to Unity, so there was
	/// nowhere for a sound to live.
	///
	/// This watches the selection rather than the keys, which is why one component serves the
	/// menu, the pause overlay and the results cards without knowing anything about any of them:
	/// however the selection moved - arrow, Tab, or the mouse passing over a button - it moved,
	/// and that is the thing worth hearing.
	///
	/// Put it beside a screen's <see cref="FirstKeySelection"/>. Do not put it on the bird select,
	/// which would then play twice.
	/// </summary>
	public class UiNavigationSound : MonoBehaviour
	{
		[Header("Sound")]
		[Tooltip("Played when the focus moves from one button to another.")]
		[SerializeField] private AudioClip m_Move;

		[Tooltip("Quieter than a press. The move is constant while a player reads a screen, and "
			+ "at full volume it wears out fast.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_Volume = 0.6f;

		private GameObject m_Before;

		/// <summary>
		/// Whether a change of selection is a move the player should hear.
		///
		/// The first selection of all is not. Every screen opens with nothing selected and hands
		/// focus to its first button on the first keypress
		/// (<see cref="FirstKeySelection"/>), so counting that as a move would make every screen
		/// in the game chirp as it arrives. The same goes for focus being cleared on the way out.
		/// </summary>
		public static bool IsAMove(GameObject i_Before, GameObject i_Now)
		{
			return i_Before != null && i_Now != null && i_Before != i_Now;
		}

		private void OnEnable()
		{
			// Starting from whatever is selected now, rather than from null, so re-enabling a
			// screen does not replay the move that put the focus there.
			m_Before = EventSystem.current == null ? null : EventSystem.current.currentSelectedGameObject;
		}

		private void Update()
		{
			if (EventSystem.current == null)
			{
				return;
			}

			GameObject now = EventSystem.current.currentSelectedGameObject;

			if (IsAMove(m_Before, now))
			{
				UiSound.Play(m_Move, m_Volume);
			}

			m_Before = now;
		}
	}
}
