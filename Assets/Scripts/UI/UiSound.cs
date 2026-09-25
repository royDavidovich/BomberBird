using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Plays a short interface clip that has to outlive the screen that started it.
	///
	/// Every menu sound worth hearing is raised by the press that leaves the screen: Play and
	/// the bird cards both load another scene at the end of the same frame, which destroys the
	/// pressed button, its AudioSource and the sound with it. A throwaway object carried across
	/// the load is the smallest thing that lets the clip finish, and it deletes itself when the
	/// clip ends.
	///
	/// A refusal does not load anything and would survive without this, but it goes through the
	/// same door so that there is one way a menu makes a noise rather than two. Being the one
	/// door is also what lets the player's effects level reach every menu sound from here.
	/// </summary>
	public static class UiSound
	{
		public static AudioSource Play(AudioClip i_Clip)
		{
			return Play(i_Clip, 1f);
		}

		/// <summary>
		/// Returns the carrier's source, for the rare caller that has to cut the clip short.
		/// Null when there was nothing to play.
		/// </summary>
		public static AudioSource Play(AudioClip i_Clip, float i_Volume)
		{
			// An unassigned slot should be silence, not an error every time the player presses
			// a button. PlayOneShot and Play both log on a null clip.
			if (i_Clip == null)
			{
				return null;
			}

			GameObject carrier = new GameObject("UiSound (" + i_Clip.name + ")");
			Object.DontDestroyOnLoad(carrier);

			AudioSource source = carrier.AddComponent<AudioSource>();
			source.clip = i_Clip;
			source.volume = i_Volume * SoundLevels.Effects;
			source.playOnAwake = false;
			source.loop = false;
			source.Play();

			Object.Destroy(carrier, i_Clip.length);

			return source;
		}
	}
}
