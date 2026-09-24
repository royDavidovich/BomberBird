using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the canopy window is found from the tree art alone: the largest see-through patch
	/// the tree closes all the way round, never the open air around the tree, and never a
	/// smaller gap between two branches.
	/// </summary>
	public class CanopyWindowTests
	{
		private static readonly Color32 sr_Clear = new Color32(0, 0, 0, 0);
		private static readonly Color32 sr_Solid = new Color32(120, 80, 40, 255);

		/// <summary>
		/// Builds pixels from rows written top-down, '#' solid and '.' clear, in the bottom-up
		/// order Texture2D.GetPixels32 hands them over.
		/// </summary>
		private static Color32[] pixels(params string[] i_RowsTopDown)
		{
			int width = i_RowsTopDown[0].Length;
			int height = i_RowsTopDown.Length;
			Color32[] result = new Color32[width * height];

			for (int row = 0; row < height; ++row)
			{
				int y = height - 1 - row;

				for (int x = 0; x < width; ++x)
				{
					result[y * width + x] = i_RowsTopDown[row][x] == '#' ? sr_Solid : sr_Clear;
				}
			}

			return result;
		}

		[Test]
		public void AHoleTheTreeClosesIsTheWindowAndTheAirAroundItIsNot()
		{
			Color32[] art = pixels(
				".....",
				".###.",
				".#.#.",
				".###.",
				".....");

			bool[] window = CanopyWindow.FindWindow(art, 5, 5);

			Assert.IsTrue(window[2 * 5 + 2], "the hole in the middle");
			Assert.IsFalse(window[0], "the open air in a corner");
			Assert.IsFalse(window[1 * 5 + 1], "the tree itself");
		}

		[Test]
		public void OnlyTheLargestClosedHoleIsTheWindow()
		{
			Color32[] art = pixels(
				"#########",
				"#...#.###",
				"#...#####",
				"#...#####",
				"#########");

			bool[] window = CanopyWindow.FindWindow(art, 9, 5);

			Assert.IsTrue(window[2 * 9 + 2], "inside the big hole");
			Assert.IsFalse(window[3 * 9 + 5], "the small gap between branches shows the backdrop instead");
		}

		[Test]
		public void AWindowSquarerThanThePictureIsCoveredEdgeToEdgeTopToBottom()
		{
			// 110 x 70 is squarer than 16:9, so the height decides and the sides overhang.
			Rect cover = CanopyWindow.CoverRect(new RectInt(20, 30, 110, 70), 16f / 9f);

			Assert.AreEqual(70f, cover.height, 0.001f);
			Assert.AreEqual(124.444f, cover.width, 0.001f);
			Assert.AreEqual(75f, cover.center.x, 0.001f, "centred on the window");
			Assert.AreEqual(65f, cover.center.y, 0.001f, "centred on the window");
		}

		[Test]
		public void AWindowWiderThanThePictureIsCoveredSideToSide()
		{
			Rect cover = CanopyWindow.CoverRect(new RectInt(0, 0, 200, 50), 16f / 9f);

			Assert.AreEqual(200f, cover.width, 0.001f);
			Assert.AreEqual(112.5f, cover.height, 0.001f);
		}

		[Test]
		public void AHoleOpenToTheEdgeIsAirNotAWindow()
		{
			Color32[] art = pixels(
				"#.###",
				"#...#",
				"#####");

			bool[] window = CanopyWindow.FindWindow(art, 5, 3);

			foreach (bool pixel in window)
			{
				Assert.IsFalse(pixel);
			}
		}
	}
}
