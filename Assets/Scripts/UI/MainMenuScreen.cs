using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The first screen. Play starts the run at the intro stage, deliberately not through the
	/// selection screen: the intro teaches the rules with the hoopoe already in hand, and a
	/// choice offered before the player knows what a bird does is not a choice.
	///
	/// The menu opens with nothing chosen, so a player reaching for the mouse is not told what
	/// to press. <see cref="FirstKeySelection"/> hands focus to Play on the first key pressed.
	/// </summary>
	public class MainMenuScreen : MonoBehaviour
	{
		[Header("Difficulty")]
		[Tooltip("The panel the Difficulty button opens. The setting is read and changed there, "
			+ "so the button only has to name what it opens.")]
		[SerializeField] private DifficultyPanel m_Difficulty;

		[Header("Sound")]
		[Tooltip("Play is pressed and the run begins. The same clip the selection screen "
			+ "confirms a bird with, because it is the same act.")]
		[SerializeField] private AudioClip m_Confirm;

		/// <summary>Wired to Play.</summary>
		public void Play()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no run to start.", this);
				return;
			}

			UiSound.Play(m_Confirm);
			GameFlow.Instance.StartStage();
		}

		/// <summary>
		/// Wired to the Difficulty button. Opens the choice rather than making it: the button
		/// used to flip the setting and report it, which meant a player had to press it to find
		/// out what else was on offer.
		/// </summary>
		public void ShowDifficulty()
		{
			if (m_Difficulty == null)
			{
				Debug.LogError(name + ": no difficulty panel wired, so the button opens nothing.", this);
				return;
			}

			m_Difficulty.Show();
		}

		/// <summary>
		/// Wired to Quit. Does nothing in the editor, which is Unity's behaviour rather than
		/// a bug worth working around.
		/// </summary>
		public void Quit()
		{
			// The clip outlives this object by design - UiSound carries it across the teardown,
			// which is the same reason Play can be heard over a scene load.
			UiSound.Play(m_Confirm);

			Application.Quit();
		}
	}
}
