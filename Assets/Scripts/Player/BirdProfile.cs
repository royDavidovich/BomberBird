using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// One playable bird: how it looks, how it handles, and the feather that unlocks it.
	///
	/// Every bird obeys the same rules. They differ only in the three tuned values here,
	/// which is what keeps the roster meaningful without any bird needing logic of its own
	/// - the exclusion GDD section 8.3 draws around "unique rule sets for every bird".
	///
	/// The values are first guesses for playtest, not balance decisions. They are on an
	/// asset precisely so changing them costs no recompile.
	/// </summary>
	[CreateAssetMenu(fileName = "bird-profile", menuName = "BomberBird/Bird Profile")]
	public class BirdProfile : ScriptableObject
	{
		[Header("Identity")]
		[Tooltip("Shown on the bird selection screen and the closing screen.")]
		[SerializeField] private string m_DisplayName = "Bird";

		[SerializeField] private BirdSpriteSet m_Sprites;

		[Header("Handling")]
		[Tooltip("Cells per second.")]
		[SerializeField] private float m_Speed = 4f;

		[Tooltip("How many cells this bird's burst reaches along each of the four directions.")]
		[SerializeField] private int m_BurstRange = 2;

		[Tooltip("How many pods this bird may have on the arena at once.")]
		[SerializeField] private int m_MaxActivePods = 1;

		[Header("Unlocking")]
		[Tooltip("The feather that represents this bird, as its shimmer frames. The starter "
			+ "bird needs none, because it is never unlocked.")]
		[SerializeField] private Sprite[] m_FeatherFrames;

		public string DisplayName
		{
			get { return m_DisplayName; }
		}

		public BirdSpriteSet Sprites
		{
			get { return m_Sprites; }
		}

		public float Speed
		{
			get { return m_Speed; }
		}

		public int BurstRange
		{
			get { return m_BurstRange; }
		}

		public int MaxActivePods
		{
			get { return m_MaxActivePods; }
		}

		/// <summary>The feather's frames, or empty for a bird that is never unlocked.</summary>
		public Sprite[] FeatherFrames
		{
			get { return m_FeatherFrames; }
		}

		/// <summary>
		/// Reports everything missing or nonsensical at once, or null when the profile is
		/// usable. A bird with no sprites or a speed of zero is a misconfigured asset, and
		/// finding that out as a silent standstill in Play Mode wastes far more time.
		/// </summary>
		public string DescribeProblems()
		{
			string problems = string.Empty;

			if (m_Sprites == null)
			{
				problems += " no sprite set;";
			}

			if (m_Speed <= 0f)
			{
				problems += " speed must be above zero;";
			}

			if (m_BurstRange < 1)
			{
				problems += " burst range must be at least 1;";
			}

			if (m_MaxActivePods < 1)
			{
				problems += " active pod limit must be at least 1;";
			}

			return problems.Length == 0 ? null : problems.Trim();
		}
	}
}
