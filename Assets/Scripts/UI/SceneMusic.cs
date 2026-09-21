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
	/// A scene without one of these keeps whatever is playing. The habitat card had no one of
	/// these until 2026-09-22, which is why the bird selection screen's loop was still going
	/// over it.
	///
	/// This is the only thing in the game that decides what plays. A second component asking
	/// for a different track in the same load would race it - Unity gives no order between two
	/// Starts - so a stage that wants its own loop says so through the campaign and is answered
	/// here rather than from the screen that reads the rest of the stage.
	/// </summary>
	public class SceneMusic : MonoBehaviour
	{
		[Tooltip("The loop this scene plays. Left empty, the scene keeps whatever was playing.")]
		[SerializeField] private AudioClip m_Track;

		[Tooltip("Tick on the screens a stage owns - its habitat card and its arena - so a "
			+ "habitat that names its own loop is heard instead of the track above. Off "
			+ "everywhere else: the menu resets the run on its way in, so it would otherwise "
			+ "open on whichever stage the fresh run points at.")]
		[SerializeField] private bool m_StageOwnsTheTrack;

		/// <summary>
		/// Which of the two tracks is heard. The stage's own wins when it has one, because a
		/// habitat naming a loop is the more specific answer; otherwise the scene's stands.
		///
		/// Both empty gives null, which <see cref="GameFlow.PlayMusic"/> reads as "change
		/// nothing" - the behaviour a scene with no track of its own already relied on.
		/// </summary>
		public static AudioClip ChooseTrack(AudioClip i_StageTrack, AudioClip i_SceneTrack)
		{
			return i_StageTrack != null ? i_StageTrack : i_SceneTrack;
		}

		private void Start()
		{
			// Start rather than Awake: GameFlow may be arriving in this same load, and its own
			// Awake is what finds the source this asks for.
			if (GameFlow.Instance == null)
			{
				return;
			}

			AudioClip stageTrack = null;

			if (m_StageOwnsTheTrack && GameFlow.Instance.CurrentStage != null)
			{
				stageTrack = GameFlow.Instance.CurrentStage.Music;
			}

			GameFlow.Instance.PlayMusic(ChooseTrack(stageTrack, m_Track));
		}
	}
}
