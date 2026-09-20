using BomberBird.Flow;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// The difficulty, offered rather than announced.
	///
	/// The menu used to carry a button reading "Mynas: normal" that flipped the setting when
	/// pressed - it stated the difficulty instead of presenting the choice, and a player had to
	/// press it to find out what else was on offer. This is the choice itself: a slider with the
	/// easiest flock on the left, the bird that stands for each stop riding the lever, and the
	/// stop names written under their own notches.
	///
	/// It changes nothing about how the difficulty is stored or reaches the arena. The value is
	/// still the one bool on <see cref="GameFlow.EasyMynas"/>, still remembered between sittings
	/// by that property's own PlayerPrefs write, and still handed to the mynas by
	/// <see cref="BomberBird.Flow.StageSetup"/> as a stage opens. This panel only moves it.
	///
	/// It is offered on the menu rather than mid-stage because the setting changes how a myna is
	/// spawned, and a stage already under way keeps the mynas it was given.
	///
	/// <see cref="InstructionsPanel"/> is the pattern being followed here - the same hide-in-Awake,
	/// the same <see cref="IsShown"/> that the screen's other keyboard reader stands down against,
	/// and the same deliberate one-frame delay on closing. The one thing it cannot copy is "any
	/// key closes": the arrows belong to the slider, so only the keys listed in
	/// <see cref="m_CloseKeys"/> put the panel away.
	/// </summary>
	public class DifficultyPanel : MonoBehaviour
	{
		[Header("Parts")]
		[Tooltip("The panel itself. Hidden in Awake, so it can be left open while it is worked on.")]
		[SerializeField] private GameObject m_Panel;

		[Tooltip("The lever. Whole numbers only, 0 on the left; its maximum is set from the "
			+ "difficulty ladder rather than the Inspector, so a third stop needs no scene edit.")]
		[SerializeField] private Slider m_Slider;

		[Tooltip("The bird standing for the chosen stop. It rides the lever: parented to the "
			+ "slider's handle in the scene, so only its sprite has to change here.")]
		[SerializeField] private Image m_Portrait;

		[Tooltip("How tall the portrait may stand. Each sprite is scaled up by whole numbers "
			+ "until it next would not fit, so the pixel art never lands on a half pixel.")]
		[SerializeField] private float m_PortraitHeight = 160f;

		[Tooltip("One per stop, left to right, in the same order as the notches. The chosen one "
			+ "is lit; the rest sit back in brown.")]
		[SerializeField] private TMP_Text[] m_StopNames;

		[Header("Portraits")]
		[Tooltip("One per stop, left to right. Easy is the myna in its hollow; Normal is the "
			+ "ordinary myna. A third would be the boss, which is why it is a list.")]
		[SerializeField] private Sprite[] m_StopPortraits;

		[Header("Colours")]
		[Tooltip("The chosen stop's name. The painted labels' light gold.")]
		[SerializeField] private Color m_ChosenName = new Color(0.957f, 0.804f, 0.475f, 1f);

		[Tooltip("The stops not chosen. The painted labels' brown.")]
		[SerializeField] private Color m_OtherName = new Color(0.471f, 0.314f, 0.173f, 1f);

		[Header("Keys")]
		[Tooltip("What puts the panel away. Deliberately not 'any key': the arrows are the "
			+ "slider's, and Tab and the mouse belong to Unity's own navigation.")]
		[SerializeField]
		private KeyCode[] m_CloseKeys = { KeyCode.Escape, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space };

		[Header("Sound")]
		[Tooltip("The same clip the rest of the menu confirms with, because it is the same act.")]
		[SerializeField] private AudioClip m_Confirm;

		private GameObject m_SelectedBefore;
		private bool m_IsClosing;

		/// <summary>
		/// Whether the panel is up and owns the keyboard. Read by <see cref="FirstKeySelection"/>
		/// on this screen, so the keystroke that closes this cannot also be read as the first
		/// press that hands focus to Play.
		/// </summary>
		public bool IsShown
		{
			get { return m_Panel != null && m_Panel.activeSelf; }
		}

		private void Awake()
		{
			// Hidden here rather than trusted to be hidden in the scene, because the panel is
			// worked on with it open and would otherwise ship that way.
			show(false);

			if (m_Slider != null)
			{
				// The ladder owns its own length. Setting this from code rather than the
				// Inspector means a third stop is one constant, not a scene edit that can be
				// forgotten while the names array already has three entries.
				m_Slider.wholeNumbers = true;
				m_Slider.minValue = 0f;
				m_Slider.maxValue = DifficultyChoice.StopCount - 1;
				m_Slider.onValueChanged.AddListener(onSliderMoved);
			}
		}

		/// <summary>Opens the panel. Wired to the menu's Difficulty button.</summary>
		public void Show()
		{
			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow, so there is no difficulty to set.", this);
				return;
			}

			UiSound.Play(m_Confirm);
			show(true);
			m_IsClosing = false;

			if (m_Slider != null)
			{
				// Opens where the player left it. SetValueWithoutNotify, because this is not the
				// player choosing anything - writing the setting back here would be harmless but
				// dishonest, and it would raise the confirm sound on the way in.
				m_Slider.SetValueWithoutNotify(DifficultyChoice.StopFor(GameFlow.Instance.EasyMynas));
			}

			showStop(currentStop());

			if (EventSystem.current != null)
			{
				// Remembered so the button that opened this gets its focus back on the way out.
				// Without it the menu is left with nothing selected and the focus arrow gone,
				// which reads as the keyboard having stopped working.
				m_SelectedBefore = EventSystem.current.currentSelectedGameObject;

				if (m_Slider != null)
				{
					EventSystem.current.SetSelectedGameObject(m_Slider.gameObject);
				}
			}
		}

		private void Update()
		{
			if (m_IsClosing)
			{
				// One frame later than the press, for the reason InstructionsPanel spells out:
				// Update order is undefined, so closing on the press itself would let another
				// reader on this screen see IsShown already false and take the same keystroke.
				close();
				return;
			}

			if (!IsShown || !UiInput.IsListening())
			{
				return;
			}

			for (int key = 0; key < m_CloseKeys.Length; ++key)
			{
				if (Input.GetKeyDown(m_CloseKeys[key]))
				{
					m_IsClosing = true;
					return;
				}
			}
		}

		private void onSliderMoved(float i_Value)
		{
			int stop = DifficultyChoice.Clamp(Mathf.RoundToInt(i_Value));

			if (GameFlow.Instance != null)
			{
				// The property is what writes PlayerPrefs, so the setting is saved the moment
				// the lever moves. There is no confirm step and deliberately no cancel: the
				// panel shows what the game will do, and it is already doing it.
				GameFlow.Instance.EasyMynas = DifficultyChoice.IsEasyAt(stop);
			}

			showStop(stop);
		}

		private int currentStop()
		{
			return m_Slider == null
				? 0
				: DifficultyChoice.Clamp(Mathf.RoundToInt(m_Slider.value));
		}

		/// <summary>
		/// Puts the bird over the chosen notch and lights that stop's name. The portrait is
		/// parented to the slider's handle in the scene, so riding the lever costs nothing here -
		/// only the sprite has to change.
		/// </summary>
		private void showStop(int i_Stop)
		{
			if (m_Portrait != null && m_StopPortraits != null && i_Stop < m_StopPortraits.Length
				&& m_StopPortraits[i_Stop] != null)
			{
				m_Portrait.sprite = m_StopPortraits[i_Stop];
				m_Portrait.rectTransform.sizeDelta = fitToSlot(m_StopPortraits[i_Stop]);
			}

			if (m_StopNames == null)
			{
				return;
			}

			for (int stop = 0; stop < m_StopNames.Length; ++stop)
			{
				if (m_StopNames[stop] == null)
				{
					continue;
				}

				m_StopNames[stop].text = DifficultyChoice.NameAt(stop).ToUpperInvariant();
				m_StopNames[stop].color = stop == i_Stop ? m_ChosenName : m_OtherName;
			}
		}

		/// <summary>
		/// The size to draw a portrait at. The stops are wildly different sprites - the hollow
		/// is 240x160, the myna a 32x32 bird - so neither a fixed box nor the native size works:
		/// one would stretch the hollow, the other would leave the myna thumbnail-sized.
		///
		/// Instead each is blown up by the largest **whole** multiple that still fits the slot.
		/// Whole numbers matter more than filling the slot exactly: this is point-filtered pixel
		/// art, and a fractional scale is what turns crisp edges into mush. At the authored
		/// sizes both stops happen to land on the same height.
		/// </summary>
		private Vector2 fitToSlot(Sprite i_Sprite)
		{
			Rect rect = i_Sprite.rect;

			if (rect.height <= 0f)
			{
				return new Vector2(rect.width, rect.height);
			}

			int scale = Mathf.Max(1, Mathf.FloorToInt(m_PortraitHeight / rect.height));

			return new Vector2(rect.width * scale, rect.height * scale);
		}

		private void close()
		{
			show(false);
			m_IsClosing = false;

			if (EventSystem.current != null && m_SelectedBefore != null)
			{
				EventSystem.current.SetSelectedGameObject(m_SelectedBefore);
			}

			m_SelectedBefore = null;
		}

		private void show(bool i_IsShown)
		{
			if (m_Panel != null)
			{
				m_Panel.SetActive(i_IsShown);
			}
		}
	}
}
