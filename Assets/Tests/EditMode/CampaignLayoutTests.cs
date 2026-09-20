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

		/// <summary>
		/// Where the bird stands in the gameplay scene. One scene serves every stage, so this
		/// is a world position rather than a cell.
		/// </summary>
		private static readonly Vector3 sr_StartWorld = new Vector3(-5f, 4f, 0f);

		/// <summary>The standard arena. The boss stage is wider, which is why this is not a rule.</summary>
		private const int k_StandardWidth = 13;
		private const int k_StandardHeight = 11;

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

		/// <summary>
		/// The cell the bird starts this stage on. Derived rather than written down, because
		/// the arena is centred on the origin: a wider stage puts the same scene position on
		/// a different cell, and the boss stage is wider.
		/// </summary>
		private static Vector2Int startCell(ArenaGrid i_Grid)
		{
			return i_Grid.WorldToCell(sr_StartWorld);
		}

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
			Vector2Int start = startCell(i_Grid);
			HashSet<Vector2Int> seen = new HashSet<Vector2Int> { start };
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			queue.Enqueue(start);

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
		private static bool isOnStartBurstCross(ArenaGrid i_Grid, Vector2Int i_Cell)
		{
			Vector2Int start = startCell(i_Grid);

			if (i_Cell.x != start.x && i_Cell.y != start.y)
			{
				return false;
			}

			return Mathf.Abs(i_Cell.x - start.x) + Mathf.Abs(i_Cell.y - start.y)
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
		public void EveryArenaIsAWorkableSize([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaGrid grid = loadCampaign().GetStage(i_StageNumber).Layout.CreateGrid();

			// Not an equality check any more. A stage is allowed its own size - the boss
			// stage is wider to give that fight room - so what is asserted is what actually
			// breaks a map rather than what merely differs from the others.
			Assert.AreEqual(1, grid.Width % 2,
				"Stage " + i_StageNumber + " is " + grid.Width + " wide. An even width puts the "
				+ "hard-block lattice against the border and closes the lanes.");
			Assert.AreEqual(1, grid.Height % 2,
				"Stage " + i_StageNumber + " is " + grid.Height + " tall, which must be odd for "
				+ "the same reason.");

			Assert.GreaterOrEqual(grid.Width, k_StandardWidth,
				"Stage " + i_StageNumber + " is narrower than the standard arena.");
			Assert.AreEqual(k_StandardHeight, grid.Height,
				"Stage " + i_StageNumber + " is not the standard height. The gameplay camera is "
				+ "fixed at 12 tiles tall, so a taller arena is drawn off screen.");
		}

		[Test]
		public void TheBirdCanEscapeItsOwnFirstPod([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaGrid grid = loadCampaign().GetStage(i_StageNumber).Layout.CreateGrid();

			Assert.IsTrue(grid.IsWalkable(startCell(grid)),
				"Stage " + i_StageNumber + " starts the bird inside a wall.");

			// Nothing is broken yet at stage start, so the escape has to use open floor.
			bool hasRefuge = false;

			foreach (Vector2Int cell in reachable(grid, false))
			{
				if (!isOnStartBurstCross(grid, cell))
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

			// The boss stage starts with nobody but the boss, which is still something to
			// defeat. What makes a stage unfinishable is having neither.
			List<Vector2Int> spawns = new List<Vector2Int>(layout.MynaSpawnCells);
			spawns.AddRange(layout.BossSpawnCells);

			Assert.IsNotEmpty(spawns,
				"Stage " + i_StageNumber + " has neither mynas nor a boss, so it cannot end.");
			Assert.AreEqual(spawns.Count, new HashSet<Vector2Int>(spawns).Count,
				"Stage " + i_StageNumber + " stacks two enemies on one cell.");

			HashSet<Vector2Int> everReachable = reachable(grid, true);
			Vector2Int start = startCell(grid);

			foreach (Vector2Int spawn in spawns)
			{
				Assert.IsTrue(grid.IsWalkable(spawn),
					"Stage " + i_StageNumber + " spawns an enemy inside a block at " + spawn + ".");
				Assert.IsTrue(everReachable.Contains(spawn),
					"Stage " + i_StageNumber + " walls an enemy off at " + spawn + ", so the arena "
					+ "can never be cleared.");

				int gap = Mathf.Abs(spawn.x - start.x) + Mathf.Abs(spawn.y - start.y);

				Assert.GreaterOrEqual(gap, k_MinSpawnDistance,
					"Stage " + i_StageNumber + " spawns an enemy " + gap + " cells from the bird "
					+ "at " + spawn + ", which kills the player as the stage loads.");
			}
		}

		/// <summary>
		/// The boss needs room to be summoned around. A boss walled into a one-cell pocket
		/// would call its slaves into cells that do not exist and the fight would never
		/// escalate.
		/// </summary>
		[Test]
		public void TheBossHasRoomAroundIt([ValueSource("StageNumbers")] int i_StageNumber)
		{
			ArenaLayout layout = loadCampaign().GetStage(i_StageNumber).Layout;
			IList<Vector2Int> bossCells = layout.BossSpawnCells;

			if (bossCells.Count == 0)
			{
				Assert.Pass("Stage " + i_StageNumber + " has no boss.");
			}

			Assert.AreEqual(1, bossCells.Count,
				"Stage " + i_StageNumber + " names more than one boss cell.");

			ArenaGrid grid = layout.CreateGrid();
			int open = 0;

			foreach (Vector2Int step in sr_Steps)
			{
				if (grid.IsWalkable(bossCells[0] + step))
				{
					++open;
				}
			}

			Assert.GreaterOrEqual(open, 2,
				"Stage " + i_StageNumber + " boxes the boss in at " + bossCells[0] + " with only "
				+ open + " open neighbour(s).");
		}

		/// <summary>
		/// Every stage can fill its habitat screen.
		///
		/// The hero art is deliberately not required: the six habitat heroes are commissioned
		/// separately and the screen falls back to the stage's own floor tile, so a stage
		/// without one looks plainer rather than broken. The words are required, because a
		/// habitat screen with no words is a picture and a press.
		/// </summary>
		[Test]
		public void EveryStageCanFillItsHabitatScreen([ValueSource("StageNumbers")] int i_StageNumber)
		{
			Campaign.Stage stage = loadCampaign().GetStage(i_StageNumber);

			Assert.IsFalse(string.IsNullOrEmpty(stage.HabitatName),
				"Stage " + i_StageNumber + " has no habitat name.");

			Assert.IsFalse(string.IsNullOrEmpty(stage.HabitatBlurb),
				"Stage " + i_StageNumber + " has no habitat blurb, so its screen would show "
				+ "a title over empty space.");

			Assert.IsFalse(string.IsNullOrEmpty(stage.BirdLine),
				"Stage " + i_StageNumber + " has no bird line. Every habitat belongs to a "
				+ "bird, the courtyard included.");
		}

		/// <summary>
		/// The campaign reports a stage missing either line rather than shipping it, which is
		/// what makes the check above worth having: the game says so at load as well.
		/// </summary>
		[Test]
		public void TheCampaignItselfIsHappyWithItsHabitatText()
		{
			Assert.IsNull(loadCampaign().DescribeProblems());
		}
	}
}
