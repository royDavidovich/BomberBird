using System.Collections.Generic;
using BomberBird.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.Tests
{
	/// <summary>
	/// The black between screens: the shape of a fade, and that the cover gets out of the way
	/// completely rather than lingering as an invisible sheet over the game.
	/// </summary>
	public class ScreenFadeTests
	{
		private readonly List<Object> r_Spawned = new List<Object>();

		[TearDown]
		public void TearDown()
		{
			for (int i = 0; i < r_Spawned.Count; ++i)
			{
				Object.DestroyImmediate(r_Spawned[i]);
			}

			r_Spawned.Clear();
		}

		[Test]
		public void TheRampRunsFromNothingToWhole()
		{
			Assert.AreEqual(0f, ScreenFade.Ramp(0f, 0.25f), 0.0001f);
			Assert.AreEqual(0.5f, ScreenFade.Ramp(0.125f, 0.25f), 0.0001f);
			Assert.AreEqual(1f, ScreenFade.Ramp(0.25f, 0.25f), 0.0001f);
		}

		[Test]
		public void TheRampNeverOvershoots()
		{
			// The loop that drives it adds unscaled deltas, which land past the end on the
			// last frame. An unclamped ramp would drive the cover past black and back out.
			Assert.AreEqual(1f, ScreenFade.Ramp(9f, 0.25f), 0.0001f);
			Assert.AreEqual(0f, ScreenFade.Ramp(-1f, 0.25f), 0.0001f);
		}

		[Test]
		public void AZeroDurationIsACutRatherThanADivideByZero()
		{
			Assert.AreEqual(1f, ScreenFade.Ramp(0f, 0f), 0.0001f);
			Assert.AreEqual(1f, ScreenFade.Ramp(0f, -0.25f), 0.0001f);
		}

		[Test]
		public void TheCoverLeavesTheScreenEntirelyAtZero()
		{
			// The trap: a cover left active at zero alpha is invisible and still a raycast
			// target, so every button under it would stop answering the mouse.
			ScreenFade fade = makeFade(out Image cover);

			fade.Cover(1f);
			Assert.IsTrue(cover.gameObject.activeSelf);
			Assert.AreEqual(1f, cover.color.a, 0.0001f);

			fade.Cover(0f);
			Assert.IsFalse(cover.gameObject.activeSelf,
				"A transparent cover must be switched off, or it swallows clicks.");
		}

		[Test]
		public void TheCoverClampsWhatItIsHanded()
		{
			ScreenFade fade = makeFade(out Image cover);

			fade.Cover(4f);

			Assert.AreEqual(1f, cover.color.a, 0.0001f);
		}

		[Test]
		public void AFadeWithNoCoverWiredIsHarmless()
		{
			// A GameFlow whose cover was never assigned falls back to hard cuts. It must not
			// throw on the way there.
			GameObject host = new GameObject("Fade with nothing wired");
			r_Spawned.Add(host);

			Assert.DoesNotThrow(() => host.AddComponent<ScreenFade>().Cover(1f));
		}

		private ScreenFade makeFade(out Image o_Cover)
		{
			GameObject host = new GameObject("Fade under test");
			r_Spawned.Add(host);

			GameObject coverObject = new GameObject("Cover", typeof(RectTransform));
			coverObject.transform.SetParent(host.transform);
			o_Cover = coverObject.AddComponent<Image>();

			ScreenFade fade = host.AddComponent<ScreenFade>();

			System.Reflection.FieldInfo field = typeof(ScreenFade).GetField(
				"m_Cover",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

			Assert.IsNotNull(field, "m_Cover was renamed; the GameFlow prefab is stale too");
			field.SetValue(fade, o_Cover);

			return fade;
		}
	}
}
