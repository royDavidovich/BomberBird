using BomberBird.UI;
using NUnit.Framework;

namespace BomberBird.Tests
{
	/// <summary>
	/// That each pod hollow on the HUD tree says what the player can still place: a hollow per
	/// pod the bird carries, full while the pod is in hand, dimmed while it is on the field.
	/// </summary>
	public class StageHudPodPipTests
	{
		[Test]
		public void ATwoPodBirdWithBothInHandShowsTwoReady()
		{
			Assert.AreEqual(ePodPip.Ready, StageHud.PodPip(0, 2, 2));
			Assert.AreEqual(ePodPip.Ready, StageHud.PodPip(1, 2, 2));
		}

		[Test]
		public void PlacingAPodDimsTheLastHollowFirst()
		{
			Assert.AreEqual(ePodPip.Ready, StageHud.PodPip(0, 2, 1));
			Assert.AreEqual(ePodPip.Spent, StageHud.PodPip(1, 2, 1));
		}

		[Test]
		public void WithEveryPodOutEveryHollowIsSpent()
		{
			Assert.AreEqual(ePodPip.Spent, StageHud.PodPip(0, 2, 0));
			Assert.AreEqual(ePodPip.Spent, StageHud.PodPip(1, 2, 0));
		}

		[Test]
		public void AOnePodBirdHidesTheSecondHollow()
		{
			Assert.AreEqual(ePodPip.Ready, StageHud.PodPip(0, 1, 1));
			Assert.AreEqual(ePodPip.Hidden, StageHud.PodPip(1, 1, 1));
			Assert.AreEqual(ePodPip.Hidden, StageHud.PodPip(1, 1, 0), "hidden whether or not the pod is out");
		}

		[Test]
		public void AnOverdrawnCountReadsAsSpentRatherThanBreaking()
		{
			Assert.AreEqual(ePodPip.Spent, StageHud.PodPip(0, 1, -1));
		}
	}
}
