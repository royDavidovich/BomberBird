using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The breathing a waiting prompt does, held in one place.
	///
	/// Four screens ask the player to press something and then wait: the habitat card, the
	/// stage-ready prompt, the closing screens and the How to play panel. A prompt that sits at
	/// full alpha reads as part of the layout rather than as something waiting on the player, so
	/// each of them fades its line in and out. That was the same cosine written out three times
	/// before this existed, and the fourth copy is where duplication stops being cheaper than a
	/// name.
	///
	/// Only the arithmetic lives here. Each screen keeps its own serialized floor and period,
	/// finds its own text and decides when its pulse starts - which differs, because a card that
	/// holds its prompt back for a moment starts counting from the moment it appears, while the
	/// stage-ready prompt is up from the first frame.
	/// </summary>
	public static class UiPulse
	{
		public static float Alpha(float i_SecondsSinceStart, float i_PulseSeconds, float i_Floor)
		{
			// A period of zero is the Inspector's way of asking for no pulse at all. Dividing by
			// it would hand the caller a NaN alpha and an invisible prompt.
			if (i_PulseSeconds <= 0f)
			{
				return 1f;
			}

			float wave = 0.5f + 0.5f * Mathf.Cos(i_SecondsSinceStart / i_PulseSeconds * 2f * Mathf.PI);

			return Mathf.Lerp(i_Floor, 1f, wave);
		}
	}
}
