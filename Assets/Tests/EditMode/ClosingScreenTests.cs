using System;
using System.Collections.Generic;
using System.Reflection;
using BomberBird.Player;
using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// The closing screen's rescued row: that it names only the birds the run actually freed,
	/// and that the cards left standing close the gaps rather than leaving holes where the
	/// hidden ones were.
	/// </summary>
	public class ClosingScreenTests
	{
		private const float k_CardWidth = 340f;
		private const float k_Spacing = 36f;
		private const float k_Step = k_CardWidth + k_Spacing;
		private const float k_Row = -40f;

		private readonly List<UnityEngine.Object> r_Spawned = new List<UnityEngine.Object>();

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
		public void ABirdThatWasNeverRescuedIsHidden()
		{
			// The bug this guards: the panel authors one card per bird and showed all of them,
			// so a player who skipped every feather still arrived at "the birds we saved
			// together" being thanked for four rescues they never made.
			ClosingScreen screen = makeScreen(4, out BirdProfile[] birds, out RectTransform[] cards);

			screen.ShowRescued(new List<BirdProfile> { birds[0], birds[2] });

			Assert.IsTrue(cards[0].gameObject.activeSelf);
			Assert.IsFalse(cards[1].gameObject.activeSelf, "The kingfisher was never freed.");
			Assert.IsTrue(cards[2].gameObject.activeSelf);
			Assert.IsFalse(cards[3].gameObject.activeSelf, "The chukar was never freed.");
		}

		[Test]
		public void TheCardsLeftStandingCloseTheGap()
		{
			ClosingScreen screen = makeScreen(4, out BirdProfile[] birds, out RectTransform[] cards);

			screen.ShowRescued(new List<BirdProfile> { birds[0], birds[3] });

			Assert.AreEqual(-k_Step * 0.5f, cards[0].anchoredPosition.x, 0.01f);
			Assert.AreEqual(k_Step * 0.5f, cards[3].anchoredPosition.x, 0.01f);
			Assert.AreEqual(k_Row, cards[0].anchoredPosition.y, 0.01f,
				"Only the row is recentred; the height the panel was authored at stands.");
		}

		[Test]
		public void OneRescuedBirdSitsInTheMiddle()
		{
			ClosingScreen screen = makeScreen(4, out BirdProfile[] birds, out RectTransform[] cards);

			screen.ShowRescued(new List<BirdProfile> { birds[1] });

			Assert.AreEqual(0f, cards[1].anchoredPosition.x, 0.01f);
		}

		[Test]
		public void NoRunShowsEveryCard()
		{
			// The scene opened on its own in the editor. Hiding three of four cards there would
			// make the panel look broken to whoever is working on it.
			ClosingScreen screen = makeScreen(4, out BirdProfile[] _, out RectTransform[] cards);

			screen.ShowRescued(null);

			for (int i = 0; i < cards.Length; ++i)
			{
				Assert.IsTrue(cards[i].gameObject.activeSelf);
			}

			Assert.AreEqual(-k_Step * 1.5f, cards[0].anchoredPosition.x, 0.01f);
			Assert.AreEqual(k_Step * 1.5f, cards[3].anchoredPosition.x, 0.01f);
		}

		private ClosingScreen makeScreen(
			int i_Count, out BirdProfile[] o_Birds, out RectTransform[] o_Cards)
		{
			GameObject host = new GameObject("Closing under test");
			r_Spawned.Add(host);

			ClosingScreen screen = host.AddComponent<ClosingScreen>();

			Type entryType = typeof(ClosingScreen)
				.GetNestedType("RescuedCard", BindingFlags.NonPublic);
			Assert.IsNotNull(entryType, "RescuedCard was renamed; the scene wiring is stale too");

			Array entries = Array.CreateInstance(entryType, i_Count);
			o_Birds = new BirdProfile[i_Count];
			o_Cards = new RectTransform[i_Count];

			for (int i = 0; i < i_Count; ++i)
			{
				o_Birds[i] = makeBird(i);
				o_Cards[i] = makeCard(host, i, i_Count);

				object entry = Activator.CreateInstance(entryType, true);
				setField(entryType, entry, "Bird", o_Birds[i]);
				setField(entryType, entry, "Card", o_Cards[i]);
				entries.SetValue(entry, i);
			}

			setField(typeof(ClosingScreen), screen, "m_RescuedCards", entries);
			setField(typeof(ClosingScreen), screen, "m_CardSpacing", k_Spacing);

			return screen;
		}

		/// <summary>A card at the spread the closing scene authors, so a test that changes
		/// nothing can still be told apart from one that recentred the row.</summary>
		private RectTransform makeCard(GameObject i_Host, int i_Index, int i_Count)
		{
			GameObject go = new GameObject("Card " + i_Index, typeof(RectTransform));
			go.transform.SetParent(i_Host.transform);

			RectTransform rect = go.GetComponent<RectTransform>();
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = new Vector2(k_CardWidth, 470f);
			rect.anchoredPosition =
				new Vector2(-k_Step * (i_Count - 1) * 0.5f + k_Step * i_Index, k_Row);

			return rect;
		}

		private BirdProfile makeBird(int i_Index)
		{
			BirdProfile bird = ScriptableObject.CreateInstance<BirdProfile>();
			bird.name = "test-bird-" + i_Index;
			r_Spawned.Add(bird);

			return bird;
		}

		private static void setField(Type i_Owner, object i_Target, string i_Name, object i_Value)
		{
			FieldInfo field = i_Owner.GetField(
				i_Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			Assert.IsNotNull(field, i_Name + " was renamed; the scene wiring is stale too");
			field.SetValue(i_Target, i_Value);
		}
	}
}
