using BomberBird.UI;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// What a key brings back after a click on empty space has cleared the selection: the
	/// button the player was last on, so the stray click does not cost them their place.
	/// </summary>
	public class FirstKeySelectionTests
	{
		private GameObject m_Last;
		private GameObject m_First;

		[SetUp]
		public void SetUp()
		{
			m_Last = new GameObject("Last button");
			m_First = new GameObject("First button");
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_Last);
			Object.DestroyImmediate(m_First);
		}

		[Test]
		public void TheButtonLastOnComesBack()
		{
			Assert.AreSame(m_Last, FirstKeySelection.ChooseRestore(m_Last, m_First));
		}

		[Test]
		public void NothingLastOnFallsBackToTheFirst()
		{
			// A screen that has just opened bare: the first key goes to its first button.
			Assert.AreSame(m_First, FirstKeySelection.ChooseRestore(null, m_First));
		}

		[Test]
		public void AHiddenButtonIsNotBroughtBack()
		{
			// Focus on a button that has since been hidden would be focus nobody can see.
			m_Last.SetActive(false);

			Assert.AreSame(m_First, FirstKeySelection.ChooseRestore(m_Last, m_First));
		}

		[Test]
		public void ADestroyedButtonIsNotBroughtBack()
		{
			Object.DestroyImmediate(m_Last);

			Assert.AreSame(m_First, FirstKeySelection.ChooseRestore(m_Last, m_First));
		}

		[Test]
		public void NeitherGivesNothing()
		{
			// The bird selection screen has no first button of its own; before a card has been
			// focused there is nothing to restore, and a key must not select a null.
			Assert.IsNull(FirstKeySelection.ChooseRestore(null, null));
		}
	}
}
