using BomberBird.Flow;
using TMPro;
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
		[Header("Mynas")]
		[Tooltip("Reads the difficulty back to the player, so the button says what the game "
			+ "will do rather than what pressing it does.")]
		[SerializeField] private TMP_Text m_MynasLabel;

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
		/// Wired to the mynas button. Flips the difficulty for every stage from here on and
		/// says so on the button.
		///
		/// It is offered on the menu rather than mid-stage because it changes how a myna is
		/// spawned, and a stage already under way keeps the mynas it was given.
		/// </summary>
		public void ToggleMynas()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no run to set the mynas for.", this);
				return;
			}

			UiSound.Play(m_Confirm);
			GameFlow.Instance.EasyMynas = !GameFlow.Instance.EasyMynas;

			showMynas();
		}

		/// <summary>
		/// Wired to Quit. Does nothing in the editor, which is Unity's behaviour rather than
		/// a bug worth working around.
		/// </summary>
		public void Quit()
		{
			Application.Quit();
		}

		private void Start()
		{
			// The setting is remembered between sittings, so the menu opens showing whichever
			// way the player left it rather than the authored default.
			showMynas();
		}

		private void showMynas()
		{
			if (m_MynasLabel == null)
			{
				return;
			}

			bool isEasy = GameFlow.Instance != null && GameFlow.Instance.EasyMynas;

			m_MynasLabel.text = isEasy ? "Mynas: easy" : "Mynas: normal";
		}
	}
}
