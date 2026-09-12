using UnityEngine;

namespace BomberBird.Arena
{
	/// <summary>The sprites for one habitat. Swapping this asset reskins an arena.</summary>
	[CreateAssetMenu(fileName = "tileset", menuName = "BomberBird/Arena Tile Set")]
	public class ArenaTileSet : ScriptableObject
	{
		[SerializeField] private Sprite m_Floor;
		[SerializeField] private Sprite m_Border;
		[SerializeField] private Sprite m_HardBlock;

		[Tooltip("Soft-block variants. One is picked per cell to break up repetition.")]
		[SerializeField] private Sprite[] m_SoftBlockVariants;

		/// <summary>
		/// The sprite for a cell. The soft-block variant is derived from the coordinate
		/// rather than drawn at random, so a stage looks identical on every run and a
		/// playtest observation stays reproducible.
		/// </summary>
		public Sprite GetSprite(eCell i_Cell, Vector2Int i_Coordinate)
		{
			Sprite sprite;

			switch (i_Cell)
			{
				case eCell.Border:
					sprite = m_Border;
					break;

				case eCell.HardBlock:
					sprite = m_HardBlock;
					break;

				case eCell.SoftBlock:
					sprite = pickSoftBlock(i_Coordinate);
					break;

				default:
					sprite = m_Floor;
					break;
			}

			return sprite;
		}

		/// <summary>Reports every missing reference at once, or null when the set is complete.</summary>
		public string DescribeMissingSprites()
		{
			string missing = string.Empty;

			if (m_Floor == null)
			{
				missing += " floor";
			}

			if (m_Border == null)
			{
				missing += " border";
			}

			if (m_HardBlock == null)
			{
				missing += " hardBlock";
			}

			if (m_SoftBlockVariants == null || m_SoftBlockVariants.Length == 0)
			{
				missing += " softBlockVariants";
			}

			return missing.Length == 0 ? null : missing.Trim();
		}

		private Sprite pickSoftBlock(Vector2Int i_Coordinate)
		{
			if (m_SoftBlockVariants == null || m_SoftBlockVariants.Length == 0)
			{
				return null;
			}

			// Two large odd primes keep neighbouring cells from landing on the same variant.
			int hash = (i_Coordinate.x * 73856093) ^ (i_Coordinate.y * 19349663);
			int index = (hash & 0x7FFFFFFF) % m_SoftBlockVariants.Length;

			return m_SoftBlockVariants[index];
		}
	}
}
