using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// Which of the two tracks a screen hears. The habitat card played the bird selection
	/// screen's loop until 2026-09-22 because nothing on it ever chose, so the choosing is
	/// worth pinning down.
	/// </summary>
	public class SceneMusicTests
	{
		private AudioClip m_StageTrack;
		private AudioClip m_SceneTrack;

		[SetUp]
		public void MakeTwoTracks()
		{
			m_StageTrack = AudioClip.Create("stage", 1, 1, 44100, false);
			m_SceneTrack = AudioClip.Create("scene", 1, 1, 44100, false);
		}

		[TearDown]
		public void DropThem()
		{
			Object.DestroyImmediate(m_StageTrack);
			Object.DestroyImmediate(m_SceneTrack);
		}

		[Test]
		public void AHabitatThatNamesALoopIsHeardOverTheScenes()
		{
			Assert.AreSame(m_StageTrack, SceneMusic.ChooseTrack(m_StageTrack, m_SceneTrack));
		}

		[Test]
		public void AHabitatWithNoLoopKeepsTheScenesTrack()
		{
			// Every stage today, which is why the arena loop still plays over all six.
			Assert.AreSame(m_SceneTrack, SceneMusic.ChooseTrack(null, m_SceneTrack));
		}

		[Test]
		public void NeitherNamingOneChangesNothing()
		{
			// Null is GameFlow.PlayMusic's own "leave it alone", so a screen that names no
			// track and belongs to no stage keeps whatever was playing - the behaviour every
			// scene without a SceneMusic already had.
			Assert.IsNull(SceneMusic.ChooseTrack(null, null));
		}
	}
}
