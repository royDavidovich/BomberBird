using System.Collections.Generic;
using BomberBird.Flow;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BomberBird.Arena.Tests
{
	/// <summary>
	/// Walks the real campaign asset and checks every handmade arena against the rules the
	/// rest of the game already assumes.
	///
	/// Six maps authored as text are six chances to make a mistake that is invisible on the
	/// page and fatal in play: a start cell walled in, a gate nothing can reach, a myna
	/// standing on the bird at frame one. Each assertion below is one of those.
	/// </summary>
	public class CampaignLayoutTests
	{
		private const string k_CampaignPath = "Assets/Settings/campaign.asset";

		/// <summary>Where the bird is placed in the gameplay scene.</summary>
		private static readonly Vector2Int sr_Start = new Vector2Int(1, 9);

		/// <summary>The longest burst in the roster, the great white pelican's.</summary>
		private const int k_LongestBurst = 3;

		/// <summary>The shortest gap that keeps a myna off the bird as the stage loads.</summary>
		private const int k_MinSpawnDistance = 3;

		private static Campaign loadCampaign()
		{
			Campaign campaign = AssetDatabase.LoadAssetAtPath<Campaign>(k_CampaignPath);

			Assert.IsNotNull(campaign, "No campaign asset at " + k_CampaignPath + ".");

			return campaign;
		}

		private static readonly Vector2Int[] sr_Steps =
			{ Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

		public static IEnumerable<int> StageNumbers()
		{
			Campaign campaign = loadCampaign();

			for (int i = 1; i <= campaign.StageCount; ++i)
			{
				yield return i;
			}
		}

		/// <summary>
		/// Every cell reachable from the start by walking, given what counts as passable.
		/// Passing a cage or a soft block as passable answers "could this ever be reached",
		/// which is the right question for the gate and the feather; passing neither answers
		/// "can this be reached right now", which is the right question for escaping a pod.
		/// </summary>
		private static HashSet<Vector2Int> reachable(ArenaGrid i_Grid, bool i_BreakablesPassable)
		{
			HashSet<Vector2Int> seen = new HashSet<Vector2Int> { sr_Start };
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			queue.Enqueue(sr_Start);

			while (queue.Count > 0)
			{
				Vector2Int cell = queue.Dequeue();

				foreach (Vector2Int step in sr_Steps)
				{
					Vector2Int next = cell + step;

					if (seen.Contains(next) || !i_Grid.IsInside(next))
					{
						continue;
					}

					eCell kind = i_Grid.GetCell(next);
					bool open = kind == eCell.Floor
						|| (i_BreakablesPassable && (kind == eCell.SoftBlock || kind == eCell.Cage));

					if (!open)
					{
						continue;
					}

					seen.Add(next);
					queue.Enqueue(next);
				}
			}

			return seen;
		}

		/// <summary>Every cell a pod on the start cell can reach, ignoring what stops it.</summary>
		private static bool isOnStartBurstCross(Vector2Int i_Cell)
		{
			if (i_Cell.x != sr_Start.x && i_Cell.y != sr_Start.y)
			{
				return false;
			}

			return Mathf.Abs(i_Cell.x - sr_Start.x) + Mathf.Abs(i_Cell.y - sr_Start.y)
				<= k_LongestBurst;
		}

		[Test]
		public void TheCampaignIsUsable()
		{
			Campaign campaign = loadCampaign();

			Assert.IsNull(campaign.DescribeProblems());
			Assert.AreEqual(6, campaign.StageCount, "The GDD calls for six handmade levels.");
			Assert.IsNotNull(campaign.StartingBird);
		}

		[Test]
		public void EveryStageHasACompleteHabitat([ValueSource("StageNumbers")] int i_StageNumber)
		{
			Campaign.Stage stage = loadCampaign().GetStage(i_StageNumber);

			Assert.IsNotNull(stage.Layout, "Stage " + i_StageNumber + " has no layout.");
			Assert.IsNotNull(stage.TileSet, "Stage " + i_StageNumber + " has no tile set.");
			Assert.IsFalse(string.IsNullOrEmpty(stage.HabitatName));
			Assert.IsNull(stage.TileSet.DescribeMissingSprites(),
				"Stage " + i_StageNumber + "'s tile set is incomplete.");
		}

		[Test]
		public void EveryArenaIsTheExpectedSize([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaGrid grid = loadCampaign().GetStage(i_StageNumber).Layout.CreateGrid();

			Assert.AreEqual(ArenaLayout.k_Width, grid.Width);
			Assert.AreEqual(ArenaLayout.k_Height, grid.Height);
		}

		[Test]
		public void TheBirdCanEscapeItsOwnFirstPod([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaGrid grid = loadCampaign().GetStage(i_StageNumber).Layout.CreateGrid();

			Assert.IsTrue(grid.IsWalkable(sr_Start),
				"Stage " + i_StageNumber + " starts the bird inside a wall.");

			// Nothing is broken yet at stage start, so the escape has to use open floor.
			bool hasRefuge = false;

			foreach (Vector2Int cell in reachable(grid, false))
			{
				if (!isOnStartBurstCross(cell))
				{
					hasRefuge = true;
					break;
				}
			}

			Assert.IsTrue(hasRefuge,
				"Stage " + i_StageNumber + " has no cell off the start's range-" + k_LongestBurst
				+ " burst cross, so a pelican placing a pod on spawn cannot survive it.");
		}

		[Test]
		public void TheGateCanBeWalkedTo([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaGrid grid = loadCampaign().GetStage(i_StageNumber).Layout.CreateGrid();
			Vector2Int approach = StageExit.GateCellFor(grid) + Vector2Int.left;

			Assert.IsTrue(reachable(grid, true).Contains(approach),
				"Stage " + i_StageNumber + " walls off " + approach + ", the cell the bird has "
				+ "to stand on to step into the gate.");
		}

		[Test]
		public void ACagedFeatherMatchesAnAwardedBird([ValueSource("StageNumbers")] int i_StageNumber)
		{
			Campaign.Stage stage = loadCampaign().GetStage(i_StageNumber);
			ArenaGrid grid = stage.Layout.CreateGrid();

			Assert.AreEqual(stage.AwardedBird != null, grid.HasCage,
				"Stage " + i_StageNumber + " disagrees with itself: a stage awards a bird if and "
				+ "only if it cages a feather.");

			if (!grid.HasCage)
			{
				return;
			}

			Assert.IsTrue(reachable(grid, true).Contains(grid.CageCell),
				"Stage " + i_StageNumber + "'s cage is walled off.");

			bool hasOpenSide = false;

			foreach (Vector2Int step in sr_Steps)
			{
				if (grid.IsWalkable(grid.CageCell + step)
					|| grid.IsDestructible(grid.CageCell + step))
				{
					hasOpenSide = true;
					break;
				}
			}

			Assert.IsTrue(hasOpenSide,
				"Stage " + i_StageNumber + "'s cage is sealed by indestructible blocks, so the "
				+ "feather can never be picked up.");
		}

		[Test]
		public void EveryMynaStartsSomewhereFair([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaLayout layout = loadCampaign().GetStage(i_StageNumber).Layout;
			ArenaGrid grid = layout.CreateGrid();
			IList<Vector2Int> spawns = layout.MynaSpawnCells;
			HashSet<Vector2Int> everReachable = reachable(grid, true);

			Assert.IsNotEmpty(spawns, "Stage " + i_StageNumber + " has no mynas, so it cannot end.");
			Assert.AreEqual(spawns.Count, new HashSet<Vector2Int>(spawns).Count,
				"Stage " + i_StageNumber + " stacks two mynas on one cell.");

			foreach (Vector2Int spawn in spawns)
			{
				Assert.IsTrue(grid.IsWalkable(spawn),
					"Stage " + i_StageNumber + " spawns a myna inside a block at " + spawn + ".");
				Assert.IsTrue(everReachable.Contains(spawn),
					"Stage " + i_StageNumber + " walls a myna off at " + spawn + ", so the arena "
					+ "can never be cleared.");

				int gap = Mathf.Abs(spawn.x - sr_Start.x) + Mathf.Abs(spawn.y - sr_Start.y);

				Assert.GreaterOrEqual(gap, k_MinSpawnDistance,
					"Stage " + i_StageNumber + " spawns a myna " + gap + " cells from the bird "
					+ "at " + spawn + ", which kills the player as the stage loads.");
			}
		}
	}
}
