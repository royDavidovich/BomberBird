using BomberBird.Arena;
using BomberBird.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// The habitat card: where this stage is, and whose place it is, shown before the arena.
	///
	/// Its own scene rather than a panel over the loaded arena, because the art is the point.
	/// A habitat reads as somewhere the player has arrived when it fills the screen, and as a
	/// label when it sits in a box over the tiles it is describing.
	///
	/// It decides nothing. Removing this scene from the build would send every stage straight
	/// into its arena and cost the campaign only its sense of place, which is the separation
	/// Docs/GDD.md section 7 asks of presentation.
	///
	/// Reached only by <see cref="GameFlow.StartStage"/>, which is arriving at a stage. A
	/// replay - a death, a Retry, the pause overlay's Restart - goes to the arena directly, so
	/// the card never stands between a player and another attempt. That rule lives in the
	/// routing rather than in a flag here: see <see cref="GameFlow.ReplayStage"/>.
	/// </summary>
	public class StageIntroScreen : MonoBehaviour
	{
		[Header("Art")]
		[Tooltip("Fills the screen. Given the habitat's hero, or its floor tile when the hero "
			+ "has not been drawn yet.")]
		[SerializeField] private Image m_Hero;

		[Tooltip("How much smaller the floor tile is drawn when it stands in for a missing "
			+ "hero. 1 tiles it at 32 px, which at this size is a texture rather than a "
			+ "backdrop. Ignored once a hero exists.")]
		[Range(0.05f, 1f)]
		[SerializeField] private float m_FallbackTileScale = 0.25f;

		[Header("Text")]
		[SerializeField] private TMP_Text m_StageLabel;

		[Tooltip("The word over the habitat's name on the first stage only, so the player "
			+ "knows the gentlest arena in the campaign is meant as one.")]
		[SerializeField] private GameObject m_IntroTag;

		[SerializeField] private TMP_Text m_HabitatName;
		[SerializeField] private TMP_Text m_BirdLine;
		[SerializeField] private TMP_Text m_Blurb;

		[Header("Prompt")]
		[Tooltip("Hidden until the player has had a moment to read.")]
		[SerializeField] private GameObject m_ContinueHint;

		[Tooltip("Seconds the card holds before a press is accepted. The player arrives here "
			+ "still pressing the key that chose their bird, and this card is two lines they "
			+ "would otherwise never see.")]
		[SerializeField] private float m_Hold = 1.6f;

		[Tooltip("Seconds for one full fade of the prompt, matching the closing screens.")]
		[SerializeField] private float m_PulseSeconds = 1.4f;

		[Tooltip("How far down the pulse fades. 1 would not move at all.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_PulseFloor = 0.3f;

		[Header("Sound")]
		[Tooltip("The same clip the rest of the game confirms with. UiSound carries it across "
			+ "the load this press starts, so the card's own dismissal is still heard.")]
		[SerializeField] private AudioClip m_Confirm;

		private float m_ShownAt;
		private TMP_Text m_HintText;

		private void Start()
		{
			// The clock is left as StartStage set it. Nothing on this screen is timed by
			// Time.time, and the arena has not loaded, so there is nothing here to freeze.
			m_ShownAt = Time.unscaledTime;

			if (m_ContinueHint != null)
			{
				m_ContinueHint.SetActive(false);
			}

			dress(GameFlow.Instance == null ? null : GameFlow.Instance.CurrentStage);
		}

		private void Update()
		{
			if (Time.unscaledTime - m_ShownAt < m_Hold)
			{
				return;
			}

			if (m_ContinueHint != null && !m_ContinueHint.activeSelf)
			{
				m_ContinueHint.SetActive(true);
			}

			pulseHint();

			if (!UiInput.WasKeyPressed())
			{
				return;
			}

			UiSound.Play(m_Confirm);

			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.BeginStage();
			}
		}

		/// <summary>
		/// Puts this stage's habitat on the card. A stage the campaign does not have leaves
		/// whatever the scene was authored with, which is a readable placeholder rather than
		/// a screen of empty labels.
		/// </summary>
		private void dress(Campaign.Stage i_Stage)
		{
			if (i_Stage == null)
			{
				Debug.LogWarning(name + ": no campaign stage to introduce. Showing the "
					+ "scene's own placeholder text.", this);
				return;
			}

			int stageNumber = GameFlow.Instance == null
				? RunState.k_FirstStage
				: GameFlow.Instance.StageNumber;

			setText(m_StageLabel, "STAGE " + StageWords.Spelled(stageNumber));

			if (m_IntroTag != null)
			{
				m_IntroTag.SetActive(stageNumber == RunState.k_FirstStage);
			}

			setText(m_HabitatName, i_Stage.HabitatName);
			setText(m_BirdLine, i_Stage.BirdLine);
			setText(m_Blurb, i_Stage.HabitatBlurb);

			showArt(i_Stage);
		}

		/// <summary>
		/// The habitat's hero, or its own floor tile repeated behind the text until that hero
		/// is drawn. The tile is the one thing on hand that is genuinely this habitat and not
		/// another's, so the placeholder is at least the right colour.
		/// </summary>
		private void showArt(Campaign.Stage i_Stage)
		{
			if (m_Hero == null)
			{
				return;
			}

			if (i_Stage.HabitatHero != null)
			{
				m_Hero.sprite = i_Stage.HabitatHero;
				m_Hero.type = Image.Type.Simple;
				m_Hero.preserveAspect = false;
				return;
			}

			if (i_Stage.TileSet == null)
			{
				return;
			}

			m_Hero.sprite = i_Stage.TileSet.GetSprite(eCell.Floor, Vector2Int.zero);
			m_Hero.type = Image.Type.Tiled;
			m_Hero.pixelsPerUnitMultiplier = m_FallbackTileScale;
		}

		private static void setText(TMP_Text i_Label, string i_Text)
		{
			if (i_Label != null && !string.IsNullOrEmpty(i_Text))
			{
				i_Label.text = i_Text;
			}
		}

		/// <summary>
		/// Breathes the prompt in and out, unscaled so it keeps moving whatever the clock is
		/// doing. The same pulse as the closing screens and the stage-ready prompt.
		/// </summary>
		private void pulseHint()
		{
			if (m_ContinueHint == null)
			{
				return;
			}

			if (m_HintText == null)
			{
				m_HintText = m_ContinueHint.GetComponent<TMP_Text>();

				if (m_HintText == null)
				{
					return;
				}
			}

			m_HintText.alpha = UiPulse.Alpha(
				Time.unscaledTime - m_ShownAt - m_Hold, m_PulseSeconds, m_PulseFloor);
		}
	}
}
