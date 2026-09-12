using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// One bird's frames. Only three facings are authored; Left is the Right sprite
	/// mirrored in engine, which is safe because the art carries no directional lighting.
	/// </summary>
	[CreateAssetMenu(fileName = "bird", menuName = "BomberBird/Bird Sprite Set")]
	public class BirdSpriteSet : ScriptableObject
	{
		[Header("Facing down, toward the camera")]
		[SerializeField] private Sprite m_DownIdle;
		[SerializeField] private Sprite[] m_DownWalk;

		[Header("Facing up, away from the camera")]
		[SerializeField] private Sprite m_UpIdle;
		[SerializeField] private Sprite[] m_UpWalk;

		[Header("Facing right. Left is this, mirrored")]
		[SerializeField] private Sprite m_SideIdle;
		[SerializeField] private Sprite[] m_SideWalk;

		/// <summary>True when the sprite for this facing must be mirrored horizontally.</summary>
		public bool IsMirrored(eFacing i_Facing)
		{
			return i_Facing == eFacing.Left;
		}

		public Sprite GetIdle(eFacing i_Facing)
		{
			Sprite sprite;

			switch (i_Facing)
			{
				case eFacing.Up:
					sprite = m_UpIdle;
					break;

				case eFacing.Down:
					sprite = m_DownIdle;
					break;

				default:
					sprite = m_SideIdle;
					break;
			}

			return sprite;
		}

		/// <summary>A walk frame, wrapping so any step index is valid.</summary>
		public Sprite GetWalk(eFacing i_Facing, int i_Step)
		{
			Sprite[] frames = getWalkFrames(i_Facing);

			if (frames == null || frames.Length == 0)
			{
				return GetIdle(i_Facing);
			}

			int index = ((i_Step % frames.Length) + frames.Length) % frames.Length;

			return frames[index];
		}

		/// <summary>Reports every missing reference at once, or null when the set is complete.</summary>
		public string DescribeMissingSprites()
		{
			string missing = string.Empty;

			if (m_DownIdle == null)
			{
				missing += " downIdle";
			}

			if (m_UpIdle == null)
			{
				missing += " upIdle";
			}

			if (m_SideIdle == null)
			{
				missing += " sideIdle";
			}

			if (m_DownWalk == null || m_DownWalk.Length == 0)
			{
				missing += " downWalk";
			}

			if (m_UpWalk == null || m_UpWalk.Length == 0)
			{
				missing += " upWalk";
			}

			if (m_SideWalk == null || m_SideWalk.Length == 0)
			{
				missing += " sideWalk";
			}

			return missing.Length == 0 ? null : missing.Trim();
		}

		private Sprite[] getWalkFrames(eFacing i_Facing)
		{
			Sprite[] frames;

			switch (i_Facing)
			{
				case eFacing.Up:
					frames = m_UpWalk;
					break;

				case eFacing.Down:
					frames = m_DownWalk;
					break;

				default:
					frames = m_SideWalk;
					break;
			}

			return frames;
		}
	}
}
