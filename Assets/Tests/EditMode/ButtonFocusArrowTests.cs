using System.Collections.Generic;
using System.Reflection;
using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the focus arrow answers to the mouse and the keyboard independently, and in
	/// particular that letting go of one while the other still holds does not hide it.
	/// </summary>
	public class ButtonFocusArrowTests
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

		/// <summary>
		/// A button carrying a hidden arrow, wired the way the scene wires it. The arrow is a
		/// serialized field with no runtime setter, so the test reaches it the same way the
		/// Inspector does.
		/// </summary>
		private ButtonFocusArrow makeButton(out GameObject o_Arrow)
		{
			GameObject host = new GameObject("Button under test");
			r_Spawned.Add(host);

			o_Arrow = new GameObject("Arrow");
			o_Arrow.transform.SetParent(host.transform);
			o_Arrow.SetActive(false);

			ButtonFocusArrow focus = host.AddComponent<ButtonFocusArrow>();
			FieldInfo field = typeof(ButtonFocusArrow)
				.GetField("m_Arrow", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(field, "m_Arrow was renamed; the scene wiring is stale too");
			field.SetValue(focus, o_Arrow);

			return focus;
		}

		[Test]
		public void PointerShowsTheArrowAndLeavingHidesIt()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnPointerEnter(null);
			Assert.IsTrue(arrow.activeSelf, "the pointer is over the button");

			focus.OnPointerExit(null);
			Assert.IsFalse(arrow.activeSelf, "the pointer has left and nothing else holds it");
		}

		[Test]
		public void SelectionShowsTheArrowAndDeselectingHidesIt()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnSelect(null);
			Assert.IsTrue(arrow.activeSelf, "the keyboard has landed on the button");

			focus.OnDeselect(null);
			Assert.IsFalse(arrow.activeSelf, "the keyboard has moved on and nothing else holds it");
		}

		[Test]
		public void LeavingWithThePointerKeepsTheArrowWhileTheButtonStaysSelected()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnPointerEnter(null);
			focus.OnSelect(null);
			focus.OnPointerExit(null);

			Assert.IsTrue(arrow.activeSelf, "the keyboard still holds this button");
		}

		[Test]
		public void DeselectingKeepsTheArrowWhileThePointerIsStillInside()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnPointerEnter(null);
			focus.OnSelect(null);
			focus.OnDeselect(null);

			Assert.IsTrue(arrow.activeSelf, "the pointer is still over this button");
		}

		[Test]
		public void ReleasingBothHidesTheArrow()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnPointerEnter(null);
			focus.OnSelect(null);
			focus.OnPointerExit(null);
			focus.OnDeselect(null);

			Assert.IsFalse(arrow.activeSelf, "nothing holds the button any more");
		}
	}
}
