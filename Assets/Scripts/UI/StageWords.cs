namespace BomberBird.UI
{
	/// <summary>
	/// The stage number as a word, for every screen that names a stage.
	///
	/// The screens are set in display faces that draw digits far less clearly than letters -
	/// Pixelify's 2 in particular reads as a Z, which is why the how-to-play step numbers were
	/// moved off it - and a campaign of six never needs a numeral. The habitat card spelled its
	/// number first; this is that same rule, held in one place so the card, the HUD, the bird
	/// selection and both results lines cannot drift apart.
	///
	/// A number beyond the words falls back to the numeral rather than losing the line.
	/// </summary>
	public static class StageWords
	{
		private static readonly string[] sr_NumberWords =
		{
			"ONE", "TWO", "THREE", "FOUR", "FIVE", "SIX"
		};

		public static string Spelled(int i_StageNumber)
		{
			if (i_StageNumber < 1 || i_StageNumber > sr_NumberWords.Length)
			{
				return i_StageNumber.ToString();
			}

			return sr_NumberWords[i_StageNumber - 1];
		}
	}
}
