using System.Collections.Generic;
using System.Reflection;
using BomberBird.Flow;
using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the panel keeps the keyboard it is given: it does not read the press that opened it
	/// as the press putting it away, and it does not let an arrow wander off the lever onto the
	/// menu still standing behind it.
	/// </summary>
	public class DifficultyPanelTests
	{
		private readonly List<GameObject> r_Spawned = new List<GameObject>();

		[TearDown]
		public void TearDown()
		{
			for (int i = 0; i < r_Spawned.Count; ++i)
			{
				UnityEngine.Object.DestroyImmediate(r_Spawned[i]);
			}

			r_Spawned.Clear();
		}

		[Test]
		public void TheOpeningPressIsNotAlsoTheClosingPress()
		{
			Assert.IsTrue(DifficultyPanel.IsOpeningFrame(120, 120),
				"the panel opened on this frame, so a close key down now is the press that opened it");
		}

		[Test]
		public void TheNextFrameBelongsToThePlayer()
		{
			Assert.IsFalse(DifficultyPanel.IsOpeningFrame(120, 121),
				"a frame later the player has had a chance to press something of their own");
		}

		[Test]
		public void APanelThatWasNeverOpenedHoldsNoFrame()
		{
			// -1 is what the field carries before the first Show, and frame counts start at 0.
			Assert.IsFalse(DifficultyPanel.IsOpeningFrame(-1, 0),
				"nothing opened, so no frame is spoken for");
		}

		[Test]
		public void TheLeverKeepsEveryArrowToItself()
		{
			Slider lever = makeLever(out DifficultyPanel panel);

			// Automatic is what the scene carried, and it resolved Up and Down onto the menu
			// buttons behind the panel: the focus left the lever and the arrows stopped moving it.
			Assert.AreEqual(Navigation.Mode.None, lever.navigation.mode,
				"an arrow must not be able to carry the focus off the lever while the panel is up");
		}

		[Test]
		public void TheLadderSetsItsOwnLength()
		{
			Slider lever = makeLever(out DifficultyPanel panel);

			Assert.IsTrue(lever.wholeNumbers, "the lever stops on notches, not between them");
			Assert.AreEqual(0f, lever.minValue, "the easiest stop is on the left");
			Assert.AreEqual(DifficultyChoice.StopCount - 1, lever.maxValue,
				"a stop added to the ladder must not need a scene edit to be reachable");
		}

		/// <summary>
		/// A panel wired to a lever the way the scene wires it, with Awake run by hand - edit
		/// mode does not run it, and it is where the panel takes the lever in hand.
		/// </summary>
		private Slider makeLever(out DifficultyPanel o_Panel)
		{
			GameObject host = new GameObject("Difficulty panel under test");
			r_Spawned.Add(host);

			Slider lever = host.AddComponent<Slider>();
			Navigation loose = lever.navigation;
			loose.mode = Navigation.Mode.Automatic;
			lever.navigation = loose;

			o_Panel = host.AddComponent<DifficultyPanel>();

			FieldInfo field = typeof(DifficultyPanel)
				.GetField("m_Slider", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(field, "m_Slider was renamed; the scene wiring is stale too");
			field.SetValue(o_Panel, lever);

			MethodInfo awake = typeof(DifficultyPanel)
				.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(awake, "Awake was renamed; the panel no longer takes the lever in hand");
			awake.Invoke(o_Panel, null);

			return lever;
		}
	}
}
