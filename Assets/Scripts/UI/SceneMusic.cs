using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// What this scene wants to hear.
	///
	/// The music plays from an AudioSource on the GameFlow object, which survives every scene
	/// load so the loop is not restarted each time a stage reloads. That is the right home for
	/// it and the reason a scene cannot simply carry its own looping source - but it also meant
	/// every screen was stuck with whatever the last one left playing. This hands the persistent
	/// source a clip as the scene opens, and <see cref="GameFlow.PlayMusic"/> ignores a request
	/// for the track already running, so a retry still does not interrupt the arena's loop.
	///
	/// A scene without one of these keeps whatever is playing.
	/// </summary>
	public class SceneMusic : MonoBehaviour
	{
		[Tooltip("The loop this scene plays. Left empty, the scene keeps whatever was playing.")]
		[SerializeField] private AudioClip m_Track;

		private void Start()
		{
			// Start rather than Awake: GameFlow may be arriving in this same load, and its own
			// Awake is what finds the source this asks for.
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.PlayMusic(m_Track);
			}
		}
	}
}
