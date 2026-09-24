using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// Masks the habitat hero to the hole in the HUD tree's canopy, worked out from the tree
	/// art itself.
	///
	/// The tree is see-through both in its canopy window and in the open air around it, and a
	/// backdrop fills that air, so the hero has to be cut to the window alone. The window is
	/// the largest see-through patch the tree closes all the way round. Finding it from the
	/// art at load means a redrawn tree brings its own window, with nothing to measure again.
	/// </summary>
	[RequireComponent(typeof(Image), typeof(Mask))]
	public class CanopyWindow : MonoBehaviour
	{
		[Tooltip("The tree art to find the window in. Its texture must have Read/Write enabled, "
			+ "or the window cannot be found and the hero stays hidden.")]
		[SerializeField] private Image m_Tree;

		[Tooltip("The habitat picture seen through the window. Sized to cover the window's box, "
			+ "so the whole picture shows bar a sliver at its sides.")]
		[SerializeField] private RectTransform m_Hero;

		private Texture2D m_MaskTexture;
		private Sprite m_MaskSprite;

		/// <summary>
		/// Which pixels are the window: the largest group of fully clear pixels that does not
		/// reach the edge of the art. Pixels run bottom row first, as
		/// <see cref="Texture2D.GetPixels32()"/> returns them. A smaller closed gap, such as the
		/// space between a branch and the sign, is left out so the backdrop shows there.
		/// </summary>
		public static bool[] FindWindow(Color32[] i_Pixels, int i_Width, int i_Height)
		{
			int count = i_Width * i_Height;
			bool[] clear = new bool[count];

			for (int i = 0; i < count; ++i)
			{
				clear[i] = i_Pixels[i].a == 0;
			}

			bool[] air = new bool[count];
			Queue<int> open = new Queue<int>();

			for (int i = 0; i < count; ++i)
			{
				int x = i % i_Width;
				int y = i / i_Width;
				bool onEdge = x == 0 || y == 0 || x == i_Width - 1 || y == i_Height - 1;

				if (onEdge && clear[i])
				{
					air[i] = true;
					open.Enqueue(i);
				}
			}

			flood(open, clear, air, i_Width, i_Height, null);

			bool[] seen = (bool[])air.Clone();
			List<int> largest = new List<int>();

			for (int i = 0; i < count; ++i)
			{
				if (!clear[i] || seen[i])
				{
					continue;
				}

				List<int> hole = new List<int>();

				seen[i] = true;
				open.Enqueue(i);
				flood(open, clear, seen, i_Width, i_Height, hole);

				if (hole.Count > largest.Count)
				{
					largest = hole;
				}
			}

			bool[] window = new bool[count];

			foreach (int pixel in largest)
			{
				window[pixel] = true;
			}

			return window;
		}

		/// <summary>
		/// The smallest rectangle of the picture's <paramref name="i_Aspect"/> that covers the
		/// window's bounding box, centred on it, in the same pixel units. Covering rather than
		/// fitting inside keeps the window full of habitat instead of showing the backdrop in
		/// thin bands along two of its edges.
		/// </summary>
		public static Rect CoverRect(RectInt i_Window, float i_Aspect)
		{
			float width = i_Window.width;
			float height = i_Window.height;

			if (width / height > i_Aspect)
			{
				height = width / i_Aspect;
			}
			else
			{
				width = height * i_Aspect;
			}

			Vector2 centre = new Vector2(i_Window.x + i_Window.width * 0.5f, i_Window.y + i_Window.height * 0.5f);

			return new Rect(centre.x - width * 0.5f, centre.y - height * 0.5f, width, height);
		}

		/// <summary>
		/// Spreads from every pixel queued to its clear, unseen neighbours, marking them seen and,
		/// when given a list, collecting them into it.
		/// </summary>
		private static void flood(Queue<int> i_Open, bool[] i_Clear, bool[] i_Seen, int i_Width, int i_Height, List<int> i_Collected)
		{
			while (i_Open.Count > 0)
			{
				int pixel = i_Open.Dequeue();
				int x = pixel % i_Width;
				int y = pixel / i_Width;

				if (i_Collected != null)
				{
					i_Collected.Add(pixel);
				}

				visit(x - 1, y, i_Open, i_Clear, i_Seen, i_Width, i_Height);
				visit(x + 1, y, i_Open, i_Clear, i_Seen, i_Width, i_Height);
				visit(x, y - 1, i_Open, i_Clear, i_Seen, i_Width, i_Height);
				visit(x, y + 1, i_Open, i_Clear, i_Seen, i_Width, i_Height);
			}
		}

		private static void visit(int i_X, int i_Y, Queue<int> i_Open, bool[] i_Clear, bool[] i_Seen, int i_Width, int i_Height)
		{
			if (i_X < 0 || i_Y < 0 || i_X >= i_Width || i_Y >= i_Height)
			{
				return;
			}

			int pixel = i_Y * i_Width + i_X;

			if (i_Clear[pixel] && !i_Seen[pixel])
			{
				i_Seen[pixel] = true;
				i_Open.Enqueue(pixel);
			}
		}

		private void Awake()
		{
			Image mask = GetComponent<Image>();
			Texture2D art = m_Tree == null || m_Tree.sprite == null ? null : m_Tree.sprite.texture;

			if (art == null || !art.isReadable)
			{
				Debug.LogError(name + ": the tree art is missing or not Read/Write enabled, so the "
					+ "canopy window cannot be found and the habitat is hidden.", this);
				gameObject.SetActive(false);
				return;
			}

			bool[] window = FindWindow(art.GetPixels32(), art.width, art.height);
			Color32[] maskPixels = new Color32[window.Length];

			for (int i = 0; i < window.Length; ++i)
			{
				maskPixels[i] = new Color32(255, 255, 255, window[i] ? (byte)255 : (byte)0);
			}

			m_MaskTexture = new Texture2D(art.width, art.height, TextureFormat.RGBA32, false);
			m_MaskTexture.filterMode = FilterMode.Point;
			m_MaskTexture.SetPixels32(maskPixels);
			m_MaskTexture.Apply(false, true);

			m_MaskSprite = Sprite.Create(m_MaskTexture, new Rect(0, 0, art.width, art.height), new Vector2(0.5f, 0.5f));
			mask.sprite = m_MaskSprite;
			GetComponent<Mask>().showMaskGraphic = false;

			fitHero(window, art.width, art.height);
		}

		/// <summary>
		/// Places the hero over the window's box, in anchors relative to the art so it scales
		/// with the tree at every screen size.
		/// </summary>
		private void fitHero(bool[] i_Window, int i_Width, int i_Height)
		{
			if (m_Hero == null)
			{
				return;
			}

			int minX = i_Width, minY = i_Height, maxX = -1, maxY = -1;

			for (int i = 0; i < i_Window.Length; ++i)
			{
				if (!i_Window[i])
				{
					continue;
				}

				int x = i % i_Width;
				int y = i / i_Width;

				minX = Mathf.Min(minX, x);
				minY = Mathf.Min(minY, y);
				maxX = Mathf.Max(maxX, x);
				maxY = Mathf.Max(maxY, y);
			}

			if (maxX < 0)
			{
				Debug.LogWarning(name + ": the tree art has no closed canopy window, so there is nowhere to show the habitat.", this);
				return;
			}

			Image heroImage = m_Hero.GetComponent<Image>();
			float aspect = heroImage != null && heroImage.sprite != null
				? heroImage.sprite.rect.width / heroImage.sprite.rect.height
				: 16f / 9f;
			Rect cover = CoverRect(new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1), aspect);

			m_Hero.anchorMin = new Vector2(cover.xMin / i_Width, cover.yMin / i_Height);
			m_Hero.anchorMax = new Vector2(cover.xMax / i_Width, cover.yMax / i_Height);
			m_Hero.offsetMin = Vector2.zero;
			m_Hero.offsetMax = Vector2.zero;
		}

		private void OnDestroy()
		{
			if (m_MaskSprite != null)
			{
				Destroy(m_MaskSprite);
			}

			if (m_MaskTexture != null)
			{
				Destroy(m_MaskTexture);
			}
		}
	}
}
