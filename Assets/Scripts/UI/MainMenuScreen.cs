using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The first screen. Play starts the run at the intro stage, deliberately not through the
	/// selection screen: the intro teaches the rules with the hoopoe already in hand, and a
	/// choice offered before the player knows what a bird does is not a choice.
	/// </summary>
	public class MainMenuScreen : MonoBehaviour
	{
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
