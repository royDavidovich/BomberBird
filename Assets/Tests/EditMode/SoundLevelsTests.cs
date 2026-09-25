using BomberBird.Flow;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// The two volume settings: full on first launch, kept once set, clamped to what a slider
	/// can mean, and back to full on a reset.
	///
	/// These write the real editor PlayerPrefs, so whatever levels the developer had are put
	/// back afterwards rather than lost to a test run.
	/// </summary>
	public class SoundLevelsTests
	{
		private float m_SavedMusic;
		private float m_SavedEffects;
		private int m_ChangedCount;

		[SetUp]
		public void SetUp()
		{
			m_SavedMusic = SoundLevels.Music;
			m_SavedEffects = SoundLevels.Effects;
			SoundLevels.ResetToDefaults();

			m_ChangedCount = 0;
			SoundLevels.Changed += countChange;
		}

		[TearDown]
		public void TearDown()
		{
			SoundLevels.Changed -= countChange;
			SoundLevels.Music = m_SavedMusic;
			SoundLevels.Effects = m_SavedEffects;
		}

		private void countChange()
		{
			++m_ChangedCount;
		}

		[Test]
		public void BothStartAtFull()
		{
			Assert.AreEqual(SoundLevels.k_DefaultLevel, SoundLevels.Music);
			Assert.AreEqual(SoundLevels.k_DefaultLevel, SoundLevels.Effects);
		}

		[Test]
		public void ALevelSetIsKeptAndTheOtherIsLeftAlone()
		{
			SoundLevels.Music = 0.3f;

			Assert.AreEqual(0.3f, SoundLevels.Music, 0.0001f);
			Assert.AreEqual(SoundLevels.k_DefaultLevel, SoundLevels.Effects);
		}

		[Test]
		public void LevelsAreClampedToASlidersRange()
		{
			SoundLevels.Music = -1f;
			SoundLevels.Effects = 2f;

			Assert.AreEqual(0f, SoundLevels.Music);
			Assert.AreEqual(1f, SoundLevels.Effects);
		}

		[Test]
		public void AResetPutsBothBackToFull()
		{
			SoundLevels.Music = 0.2f;
			SoundLevels.Effects = 0.4f;

			SoundLevels.ResetToDefaults();

			Assert.AreEqual(SoundLevels.k_DefaultLevel, SoundLevels.Music);
			Assert.AreEqual(SoundLevels.k_DefaultLevel, SoundLevels.Effects);
		}

		[Test]
		public void EveryChangeIsAnnounced()
		{
			// The playing music follows the slider through this event, so a change it missed
			// would be a slider that does nothing until the next scene.
			SoundLevels.Music = 0.5f;
			SoundLevels.Effects = 0.5f;
			SoundLevels.ResetToDefaults();

			Assert.AreEqual(3, m_ChangedCount);
		}
	}
}
