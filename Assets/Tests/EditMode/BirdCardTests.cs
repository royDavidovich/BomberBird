using System.Collections.Generic;
using System.Reflection;
using BomberBird.Flow;
using BomberBird.Player;
using BomberBird.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberBird.Tests
{
	/// <summary>
	/// The selection card: that a bird you have not earned can still be walked onto and read,
	/// that focus answers to the mouse and the keyboard independently, and that a locked card
	/// keeps the rows its neighbours use.
	/// </summary>
	public class BirdCardTests
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
		public void LockedCardStaysInteractable()
		{
			// The trap this guards: Unity's Selectable.FindSelectable skips anything that is
			// not interactable, so turning the Button off to stop the press would also take the
			// card out of navigation - and the habitat clue on it could never be read.
			BirdCard card = makeCard(false, out Parts parts);

			Assert.IsTrue(parts.Button.interactable,
				"A locked card must stay interactable or the cursor cannot reach it.");
		}

		[Test]
		public void PressingALockedCardRaisesPressedAnyway()
		{
			// The card reports the press and lets the screen decide. That is what leaves room
			// for a refusal the player can hear.
			BirdCard card = makeCard(false, out Parts parts);

			int presses = 0;
			card.Pressed += _ => ++presses;
			parts.Button.onClick.Invoke();

			Assert.AreEqual(1, presses);
			Assert.IsFalse(card.IsUnlocked);
		}

		[Test]
		public void LockedCardHidesItsNameAndValuesButKeepsItsHabitat()
		{
			BirdCard card = makeCard(false, out Parts parts);

			Assert.AreEqual("?", parts.Name.text);
			Assert.AreEqual("-", parts.Speed.text,
				"A locked card keeps its three rows and dashes the values, so the habitat "
				+ "below lands on the same line as the neighbouring cards'.");
			Assert.AreEqual("-", parts.Burst.text);
			Assert.AreEqual("-", parts.Pods.text);
			Assert.AreEqual("Lagoon Shore", parts.Habitat.text);
			Assert.IsTrue(parts.Scrim.activeSelf);
		}

		[Test]
		public void UnlockedCardShowsItsNameAndValues()
		{
			BirdCard card = makeCard(true, out Parts parts);

			Assert.AreEqual("Test bird", parts.Name.text);
			Assert.AreEqual(card.Bird.Speed.ToString(), parts.Speed.text);
			Assert.AreEqual(card.Bird.BurstRange.ToString(), parts.Burst.text);
			Assert.AreEqual(card.Bird.MaxActivePods.ToString(), parts.Pods.text);
			Assert.IsFalse(parts.Scrim.activeSelf);
		}

		[Test]
		public void FocusLiftsTheCardAndReturnsItExactly()
		{
			BirdCard card = makeCard(true, out Parts parts);
			RectTransform rect = card.transform as RectTransform;
			Vector2 resting = rect.anchoredPosition;

			card.OnSelect(null);
			Assert.Greater(rect.anchoredPosition.y, resting.y, "Focus should lift the card.");
			Assert.IsTrue(parts.Bracket.activeSelf);

			card.OnDeselect(null);
			Assert.AreEqual(resting, rect.anchoredPosition,
				"Losing focus must put the card back where it started, not near it.");
			Assert.IsFalse(parts.Bracket.activeSelf);
		}

		/// <summary>
		/// Selection is the only thing that lifts a card. Hover used to count as well, and the
		/// screen could show two chosen birds at once: arrow onto one, leave the mouse resting
		/// on another. Handling the pointer here is what caused that, so the card must not go
		/// back to listening for it - clicking still works, through the selection a click makes.
		/// </summary>
		[Test]
		public void DoesNotTakeFocusFromTheMouse()
		{
			BirdCard card = makeCard(true, out Parts parts);

			Assert.IsFalse(card is IPointerEnterHandler, "a hovered card must not light up");
			Assert.IsFalse(card is IPointerExitHandler, "a hovered card must not light up");

			card.OnSelect(null);
			Assert.IsTrue(parts.Bracket.activeSelf);

			card.OnDeselect(null);
			Assert.IsFalse(parts.Bracket.activeSelf);
		}

		[Test]
		public void LockedCardStillTakesFocus()
		{
			BirdCard card = makeCard(false, out Parts parts);

			card.OnSelect(null);

			Assert.IsTrue(parts.Bracket.activeSelf,
				"A locked card shows focus at full strength; the scrim sits under the bracket.");
		}

		private struct Parts
		{
			public Button Button;
			public Image Portrait;
			public TMP_Text Name;
			public TMP_Text Speed;
			public TMP_Text Burst;
			public TMP_Text Pods;
			public TMP_Text Habitat;
			public GameObject Bracket;
			public GameObject Scrim;
		}

		/// <summary>
		/// A card wired the way the prefab wires it. The parts are serialized fields with no
		/// runtime setters, so the test reaches them the same way the Inspector does.
		/// </summary>
		private BirdCard makeCard(bool i_IsUnlocked, out Parts o_Parts)
		{
			GameObject host = new GameObject("Card under test", typeof(RectTransform));
			r_Spawned.Add(host);

			o_Parts = new Parts
			{
				Button = host.AddComponent<Button>(),
				Portrait = child<Image>(host, "Portrait"),
				Name = child<TextMeshProUGUI>(host, "Name"),
				Speed = child<TextMeshProUGUI>(host, "SpeedValue"),
				Burst = child<TextMeshProUGUI>(host, "BurstValue"),
				Pods = child<TextMeshProUGUI>(host, "PodsValue"),
				Habitat = child<TextMeshProUGUI>(host, "Habitat"),
				Bracket = childObject(host, "Bracket"),
				Scrim = childObject(host, "Scrim"),
			};

			BirdCard card = host.AddComponent<BirdCard>();

			assign(card, "m_Button", o_Parts.Button);
			assign(card, "m_Portrait", o_Parts.Portrait);
			assign(card, "m_NameLabel", o_Parts.Name);
			assign(card, "m_SpeedValue", o_Parts.Speed);
			assign(card, "m_BurstValue", o_Parts.Burst);
			assign(card, "m_PodsValue", o_Parts.Pods);
			assign(card, "m_HabitatLabel", o_Parts.Habitat);
			assign(card, "m_FocusBracket", o_Parts.Bracket);
			assign(card, "m_LockedScrim", o_Parts.Scrim);

			// Show comes after the wiring, the order the screen uses when it builds a card.
			card.Show(new Campaign.RosterEntry(makeBird(), "Lagoon Shore"), i_IsUnlocked);

			return card;
		}

		private BirdProfile makeBird()
		{
			BirdProfile bird = ScriptableObject.CreateInstance<BirdProfile>();
			bird.name = "test-bird";
			r_Spawned.Add(bird);

			FieldInfo field = typeof(BirdProfile)
				.GetField("m_DisplayName", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(field, "m_DisplayName was renamed; the bird assets are stale too");
			field.SetValue(bird, "Test bird");

			return bird;
		}

		private T child<T>(GameObject i_Host, string i_Name) where T : Component
		{
			GameObject go = childObject(i_Host, i_Name);
			return go.AddComponent<T>();
		}

		private GameObject childObject(GameObject i_Host, string i_Name)
		{
			GameObject go = new GameObject(i_Name, typeof(RectTransform));
			go.transform.SetParent(i_Host.transform);
			return go;
		}

		private void assign(BirdCard i_Card, string i_Field, Object i_Value)
		{
			FieldInfo field = typeof(BirdCard)
				.GetField(i_Field, BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.IsNotNull(field, i_Field + " was renamed; the prefab wiring is stale too");

			field.SetValue(i_Card, i_Value);
		}
	}
}
