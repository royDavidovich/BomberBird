using System;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The player's two volume settings, music and effects, set on the pause overlay and
	/// remembered between sittings.
	///
	/// A level multiplied in where each kind of sound is played rather than an AudioMixer:
	/// the music has one source (<see cref="GameFlow"/>) and every effect goes through
	/// <c>UiSound</c> or <c>StageAudio</c>, so two numbers reach everything a mixer would.
	///
	/// PlayerPrefs is the right home: this is a preference, which the ban in
	/// <see cref="RunState"/> on authoritative progress does not cover. Read on every use rather
	/// than cached, so there is no copy to fall out of step with the stored value.
	/// </summary>
	public static class SoundLevels
	{
		public const float k_DefaultLevel = 1f;

		private const string k_MusicPref = "BomberBird.MusicLevel";
		private const string k_EffectsPref = "BomberBird.EffectsLevel";

		/// <summary>Raised whenever either level changes, so playing music can follow it.</summary>
		public static event Action Changed;

		public static float Music
		{
			get { return PlayerPrefs.GetFloat(k_MusicPref, k_DefaultLevel); }
			set { store(k_MusicPref, value); }
		}

		public static float Effects
		{
			get { return PlayerPrefs.GetFloat(k_EffectsPref, k_DefaultLevel); }
			set { store(k_EffectsPref, value); }
		}

		/// <summary>
		/// Writes the levels through to disk. Called when the player is done with the sliders,
		/// not on every step of one: a Web build only reaches browser storage on Save, and
		/// closing the tab is not a quit.
		/// </summary>
		public static void Save()
		{
			PlayerPrefs.Save();
		}

		/// <summary>Back to full on both, as on first launch. The development reset path.</summary>
		public static void ResetToDefaults()
		{
			PlayerPrefs.DeleteKey(k_MusicPref);
			PlayerPrefs.DeleteKey(k_EffectsPref);
			Changed?.Invoke();
		}

		private static void store(string i_Key, float i_Level)
		{
			PlayerPrefs.SetFloat(i_Key, Mathf.Clamp01(i_Level));
			Changed?.Invoke();
		}
	}
}
