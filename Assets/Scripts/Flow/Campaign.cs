using System;
using System.Collections.Generic;
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

			[Tooltip("The habitat this stage is drawn with. Two stages may share one set.")]
			[SerializeField] private ArenaTileSet m_TileSet;

			[Header("The habitat screen")]
			[Tooltip("The full-bleed art behind the habitat screen. Leave empty until the "
				+ "habitat's hero is drawn: the screen falls back to this stage's own floor "
				+ "tile rather than showing nothing.")]
			[SerializeField] private Sprite m_HabitatHero;

			[Tooltip("Two lines on what this habitat is. Read once, before the stage, so it "
				+ "says what the place is rather than how to play it.")]
			[TextArea(2, 4)]
			[SerializeField] private string m_HabitatBlurb;

			[Tooltip("The whole sentence naming the bird this habitat belongs to, such as "
				+ "\"home of the Eurasian hoopoe\". Authored rather than built from the "
				+ "awarded bird, because two stages belong to the hoopoe while awarding "
				+ "nothing, and the courtyard belongs to the myna, which has no profile at "
				+ "all and is not a home but a taking.")]
			[SerializeField] private string m_BirdLine;

			[Tooltip("The bird this stage's feather unlocks. Leave empty for the intro and "
				+ "the boss, which award none and open their exit as soon as the arena is clear.")]
			[SerializeField] private BirdProfile m_AwardedBird;

			[Header("Sound")]
			[Tooltip("The loop this habitat plays, from its card until the next habitat's "
				+ "card - so the card, the stage, a death and its replay all sit under one "
				+ "unbroken piece. Leave empty and the stage keeps the track the scene names, "
				+ "which is what all six do today.")]
			[SerializeField] private AudioClip m_Music;

			public string HabitatName
			{
				get { return m_HabitatName; }
			}

			public ArenaLayout Layout
			{
				get { return m_Layout; }
			}

			public ArenaTileSet TileSet
			{
				get { return m_TileSet; }
			}

			/// <summary>The bird this stage awards, or null when it awards none.</summary>
			public BirdProfile AwardedBird
			{
				get { return m_AwardedBird; }
			}

			/// <summary>The habitat screen's art, or null while it is still to be drawn.</summary>
			public Sprite HabitatHero
			{
				get { return m_HabitatHero; }
			}

			/// <summary>Two lines on what this habitat is.</summary>
			public string HabitatBlurb
			{
				get { return m_HabitatBlurb; }
			}

			/// <summary>The sentence naming the bird this habitat belongs to.</summary>
			public string BirdLine
			{
				get { return m_BirdLine; }
			}

			/// <summary>
			/// This habitat's own loop, or null when it has none and the scene's track stands.
			/// Read by <see cref="BomberBird.UI.SceneMusic"/> on the two screens a stage owns.
			/// </summary>
			public AudioClip Music
			{
				get { return m_Music; }
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

		/// <summary>One bird the campaign can hand out, and where it comes from.</summary>
		public class RosterEntry
		{
			private readonly BirdProfile r_Bird;
			private readonly string r_Habitat;

			public BirdProfile Bird
			{
				get { return r_Bird; }
			}

			/// <summary>The habitat whose feather awards it, or null for the starter.</summary>
			public string Habitat
			{
				get { return r_Habitat; }
			}

			public RosterEntry(BirdProfile i_Bird, string i_Habitat)
			{
				r_Bird = i_Bird;
				r_Habitat = i_Habitat;
			}
		}

		/// <summary>
		/// Every bird the campaign can hand out, in the order it hands them out.
		///
		/// Deliberately not <see cref="RunState.Roster"/>, which holds only what has been
		/// earned. The selection screen shows the birds still locked as well, because seeing
		/// what is coming is what makes a feather worth chasing.
		/// </summary>
		public IList<RosterEntry> EveryBird()
		{
			List<RosterEntry> birds = new List<RosterEntry>();

			if (m_StartingBird != null)
			{
				birds.Add(new RosterEntry(m_StartingBird, null));
			}

			for (int i = 0; i < StageCount; ++i)
			{
				Stage stage = m_Stages[i];

				// Two stages awarding one bird would otherwise show two cards for it.
				if (stage == null || stage.AwardedBird == null || holds(birds, stage.AwardedBird))
				{
					continue;
				}

				birds.Add(new RosterEntry(stage.AwardedBird, stage.HabitatName));
			}

			return birds;
		}

		private static bool holds(IList<RosterEntry> i_Birds, BirdProfile i_Bird)
		{
			bool found = false;

			for (int i = 0; i < i_Birds.Count && !found; ++i)
			{
				found = i_Birds[i].Bird == i_Bird;
			}

			return found;
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
				else if (m_Stages[i].TileSet == null)
				{
					problems += " stage " + (i + 1) + " has no tile set;";
				}
				else if (string.IsNullOrEmpty(m_Stages[i].HabitatBlurb))
				{
					problems += " stage " + (i + 1) + " has no habitat blurb;";
				}
				else if (string.IsNullOrEmpty(m_Stages[i].BirdLine))
				{
					problems += " stage " + (i + 1) + " has no bird line;";
				}

				// HabitatHero is deliberately not checked. The six habitat heroes are
				// commissioned art that lands after this ships, and the habitat screen
				// already falls back to the stage's floor tile, so a missing one is a
				// stage that looks plainer rather than a stage that is broken.
			}

			return problems.Length == 0 ? null : problems.Trim();
		}
	}
}
