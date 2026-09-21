using System.Collections.Generic;
using System.Reflection;
using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the focus arrow marks the selected button and nothing else.
	///
	/// It used to answer the pointer as well, which put two arrows on the menu whenever the
	/// mouse rested on one button while the keyboard had moved to another.
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
		public void SelectionShowsTheArrowAndDeselectingHidesIt()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnSelect(null);
			Assert.IsTrue(arrow.activeSelf, "the keyboard has landed on the button");

			focus.OnDeselect(null);
			Assert.IsFalse(arrow.activeSelf, "the keyboard has moved on");
		}

		[Test]
		public void OnlyTheSelectedButtonWearsAnArrow()
		{
			// The menu is driven by the keyboard, and there is one selection, so there must be one
			// arrow. When the arrow also answered the pointer, a mouse resting on one button while
			// the keyboard sat on another put two on screen and the menu read as broken.
			ButtonFocusArrow hovered = makeButton(out GameObject hoveredArrow);
			ButtonFocusArrow selected = makeButton(out GameObject selectedArrow);

			selected.OnSelect(null);

			Assert.IsTrue(selectedArrow.activeSelf, "this is the button Return would press");
			Assert.IsFalse(hoveredArrow.activeSelf,
				"the pointer being over a button is not what Return acts on");
		}

		[Test]
		public void AButtonHiddenWhileSelectedComesBackWithoutAnArrow()
		{
			ButtonFocusArrow focus = makeButton(out GameObject arrow);

			focus.OnSelect(null);

			// Edit mode does not run the lifecycle for a plain MonoBehaviour, so the callback is
			// invoked the way the Inspector reaches anything else private here.
			MethodInfo disable = typeof(ButtonFocusArrow)
				.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(disable, "OnDisable was renamed; a hidden button would keep its arrow");
			disable.Invoke(focus, null);

			Assert.IsFalse(arrow.activeSelf, "a hidden button never hears its deselect");
		}

	}
}
