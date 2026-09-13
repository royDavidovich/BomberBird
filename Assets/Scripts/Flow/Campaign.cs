using System;
using BomberBird.Arena;
using BomberBird.Player;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// The six-level campaign in order: which arena each stage uses, which habitat it is
	/// called, and which bird its feather unlocks.
	///
	/// This is the one place the stage number means anything. <see cref="ArenaLayout"/>
	/// stays pure arena data and knows nothing about birds, which it cannot anyway - the
	/// arena assembly sits below the player's, so a layout pointing at a
	/// <see cref="BirdProfile"/> would invert that dependency.
	/// </summary>
	[CreateAssetMenu(fileName = "campaign", menuName = "BomberBird/Campaign")]
	public class Campaign : ScriptableObject
	{
		/// <summary>One level of the campaign.</summary>
		[Serializable]
		public class Stage
		{
			[Tooltip("Shown on the habitat card before the stage starts.")]
			[SerializeField] private string m_HabitatName = "Habitat";

			[SerializeField] private ArenaLayout m_Layout;

			[Tooltip("The bird this stage's feather unlocks. Leave empty for the intro and "
				+ "the boss, which award none and open their exit as soon as the arena is clear.")]
			[SerializeField] private BirdProfile m_AwardedBird;

			public string HabitatName
			{
				get { return m_HabitatName; }
			}

			public ArenaLayout Layout
			{
				get { return m_Layout; }
			}

			/// <summary>The bird this stage awards, or null when it awards none.</summary>
			public BirdProfile AwardedBird
			{
				get { return m_AwardedBird; }
			}
		}

		[SerializeField] private Stage[] m_Stages;

		[Header("The bird the player starts with")]
		[Tooltip("Never unlocked, because the player already has it.")]
		[SerializeField] private BirdProfile m_StartingBird;

		public int StageCount
		{
			get { return m_Stages == null ? 0 : m_Stages.Length; }
		}

		public BirdProfile StartingBird
		{
			get { return m_StartingBird; }
		}

		/// <summary>
		/// The stage a player-facing stage number refers to, counting from 1, or null when
		/// the number runs past the end of the campaign - which is how the run knows the
		/// player has finished rather than by counting stages itself.
		/// </summary>
		public Stage GetStage(int i_StageNumber)
		{
			int index = i_StageNumber - 1;

			return index < 0 || index >= StageCount ? null : m_Stages[index];
		}

		/// <summary>Reports every misconfigured stage at once, or null when all are usable.</summary>
		public string DescribeProblems()
		{
			string problems = string.Empty;

			if (m_StartingBird == null)
			{
				problems += " no starting bird;";
			}

			if (StageCount == 0)
			{
				problems += " no stages;";
			}

			for (int i = 0; i < StageCount; ++i)
			{
				if (m_Stages[i] == null || m_Stages[i].Layout == null)
				{
					problems += " stage " + (i + 1) + " has no layout;";
				}
			}

			return problems.Length == 0 ? null : problems.Trim();
		}
	}
}
