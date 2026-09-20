using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// That the navigation click only plays for a real move between two buttons - in particular
	/// not for the first selection a screen makes, which every screen makes as it opens.
	/// </summary>
	public class UiNavigationSoundTests
	{
		private GameObject m_First;
		private GameObject m_Second;

		[SetUp]
		public void SetUp()
		{
			m_First = new GameObject("First button");
			m_Second = new GameObject("Second button");
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_First);
			Object.DestroyImmediate(m_Second);
		}

		[Test]
		public void MovingBetweenTwoButtonsIsAMove()
		{
			Assert.IsTrue(UiNavigationSound.IsAMove(m_First, m_Second));
		}

		[Test]
		public void TheFirstSelectionOfAllIsNot()
		{
			// Every screen opens with nothing selected. If this counted, the whole game would
			// chirp on arriving at any screen.
			Assert.IsFalse(UiNavigationSound.IsAMove(null, m_First));
		}

		[Test]
		public void LosingFocusIsNot()
		{
			Assert.IsFalse(UiNavigationSound.IsAMove(m_First, null));
		}

		[Test]
		public void StayingPutIsNot()
		{
			Assert.IsFalse(UiNavigationSound.IsAMove(m_First, m_First));
		}

		[Test]
		public void ADestroyedButtonCountsAsNothingSelected()
		{
			// A screen can be torn down between frames. Unity's fake null has to be respected
			// here, or the last move of a dying screen plays over the one replacing it.
			Object.DestroyImmediate(m_Second);

			Assert.IsFalse(UiNavigationSound.IsAMove(m_First, m_Second));
			Assert.IsFalse(UiNavigationSound.IsAMove(m_Second, m_First));
		}
	}
}
