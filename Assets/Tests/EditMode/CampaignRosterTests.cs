using System.Collections.Generic;
using BomberBird.Flow;
using BomberBird.Player;
using NUnit.Framework;
using UnityEditor;

namespace BomberBird.Tests
{
	/// <summary>
	/// The roster the selection screen draws from: every bird the campaign can hand out,
	/// including the ones the player has not earned yet.
	///
	/// Checked against the real campaign asset rather than a built one, because the thing
	/// worth catching is a stage whose awarded bird was cleared in the Inspector - which a
	/// hand-built fixture would never notice.
	/// </summary>
	public class CampaignRosterTests
	{
		private const string k_CampaignPath = "Assets/Settings/campaign.asset";

		private static Campaign loadCampaign()
		{
			Campaign campaign = AssetDatabase.LoadAssetAtPath<Campaign>(k_CampaignPath);

			Assert.IsNotNull(campaign, "No campaign asset at " + k_CampaignPath + ".");

			return campaign;
		}

		[Test]
		public void TheRosterHoldsEveryBirdTheCampaignCanHandOut()
		{
			Assert.AreEqual(4, loadCampaign().EveryBird().Count,
				"The GDD's playable roster is four birds: the starter plus three feathers.");
		}

		/// <summary>The starter is owned from the first frame, so no habitat awards it.</summary>
		[Test]
		public void TheStarterComesFirstAndNamesNoHabitat()
		{
			Campaign campaign = loadCampaign();
			Campaign.RosterEntry first = campaign.EveryBird()[0];

			Assert.AreSame(campaign.StartingBird, first.Bird);
			Assert.IsNull(first.Habitat, "The starter is not awarded by a stage.");
		}

		[Test]
		public void EveryEarnedBirdNamesTheHabitatThatAwardsIt()
		{
			Campaign campaign = loadCampaign();
			IList<Campaign.RosterEntry> roster = campaign.EveryBird();

			for (int i = 1; i < roster.Count; ++i)
			{
				Assert.IsNotEmpty(roster[i].Habitat,
					roster[i].Bird.name + " is awarded by a stage, so it must name one.");
			}
		}

		/// <summary>Campaign order, so the screen reads as the journey rather than a set.</summary>
		[Test]
		public void TheRosterFollowsCampaignOrder()
		{
			Campaign campaign = loadCampaign();
			IList<Campaign.RosterEntry> roster = campaign.EveryBird();
			int next = 1;

			for (int stage = 1; stage <= campaign.StageCount; ++stage)
			{
				Campaign.Stage current = campaign.GetStage(stage);

				if (current.AwardedBird == null)
				{
					continue;
				}

				Assert.AreSame(current.AwardedBird, roster[next].Bird,
					"Stage " + stage + " awards a bird out of roster order.");
				Assert.AreEqual(current.HabitatName, roster[next].Habitat);
				++next;
			}

			Assert.AreEqual(roster.Count, next, "A stage's bird is missing from the roster.");
		}

		[Test]
		public void NoBirdAppearsTwice()
		{
			IList<Campaign.RosterEntry> roster = loadCampaign().EveryBird();
			HashSet<BirdProfile> seen = new HashSet<BirdProfile>();

			foreach (Campaign.RosterEntry entry in roster)
			{
				Assert.IsTrue(seen.Add(entry.Bird),
					entry.Bird.name + " appears twice, so the screen would show two cards for it.");
			}
		}
	}
}
