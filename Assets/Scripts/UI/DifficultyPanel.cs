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
	/// key closes": the arrows belong to the lever, so the keys are split by what they mean -
	/// <see cref="m_ChooseKeys"/> takes the stop the lever is standing on, <see cref="m_CancelKeys"/>
	/// leaves the setting as it was found.
	///
	/// Moving the lever decides nothing. It used to write the setting on every move, which made
	/// looking at a stop the same act as choosing it; now the ladder can be walked and read, and
	/// the difficulty changes only when the player says so.
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
		[Tooltip("What takes the difficulty the lever is standing on and closes the panel. "
			+ "Deliberately only Return: the arrows move the lever, and the choice is not made "
			+ "until the player says so.")]
		[SerializeField]
		private KeyCode[] m_ChooseKeys = { KeyCode.Return, KeyCode.KeypadEnter };

		[Tooltip("What closes the panel without taking what the lever is standing on. The "
			+ "setting is left as it was found.")]
		[SerializeField]
		private KeyCode[] m_CancelKeys = { KeyCode.Escape };

		[Header("Sound")]
		[Tooltip("The same clip the rest of the menu confirms with, because it is the same act. "
			+ "Played on opening the panel and on putting it away.")]
		[SerializeField] private AudioClip m_Confirm;

		[Tooltip("The lever moving from one stop to the next. The same clip the focus arrow uses "
			+ "elsewhere - moving the lever is the same act as moving between buttons.")]
		[SerializeField] private AudioClip m_Move;

		[Tooltip("Quieter than a press, matching the navigation click on the other screens.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_MoveVolume = 0.6f;

		private GameObject m_SelectedBefore;
		private bool m_IsClosing;
		private bool m_Armed;

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
			// Switched on once here and straight back off, because the *first* time this panel
			// is switched on it costs over a second - measured at 1227ms, against 0.3ms for
			// every activation after it. Unity pays for the panel's materials and shader
			// variants the first time they are drawn, and until now the player paid it, by
			// pressing Difficulty: the menu froze for a second and the confirm sound the same
			// press had just started was destroyed inside that one frozen frame, unheard. The
			// button looked like the only one in the game with no sound.
			//
			// Paid here it is invisible, because the screen is still arriving behind the fade.
			// Play and Quit never showed this: neither switches anything on.
			show(true);
			Canvas.ForceUpdateCanvases();

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

				// The lever is the only thing the player is offered while the panel is up, but
				// the menu's buttons are still standing behind it, and Unity's automatic
				// navigation happily carries Up and Down onto them: the focus leaves the lever,
				// the arrows stop moving it, the focus arrow lights up behind the panel, and
				// Return presses a button nobody can see. None holds every arrow on the slider.
				// Left and Right still move the value, because a Slider only hands an arrow to
				// navigation when there is something to navigate to.
				Navigation nav = m_Slider.navigation;
				nav.mode = Navigation.Mode.None;
				m_Slider.navigation = nav;
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

			// The key that opened this is very likely still down. Nothing is taken until it is
			// let go - see IsArmedNow.
			m_Armed = false;

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
					holdTheNavigationClick();
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

			// Armed by the opening key being let go, and armed it stays. Reading the keyboard
			// before that would take the press that opened the panel.
			m_Armed = IsArmedNow(m_Armed, isAnyCloseKeyHeld());

			if (!m_Armed)
			{
				return;
			}

			for (int key = 0; key < m_ChooseKeys.Length; ++key)
			{
				if (Input.GetKeyDown(m_ChooseKeys[key]))
				{
					take();
					m_IsClosing = true;
					return;
				}
			}

			for (int key = 0; key < m_CancelKeys.Length; ++key)
			{
				if (Input.GetKeyDown(m_CancelKeys[key]))
				{
					m_IsClosing = true;
					return;
				}
			}
		}

		/// <summary>
		/// Takes the difficulty the lever is standing on.
		///
		/// The panel used to write the setting the moment the lever moved, on the grounds that
		/// it showed what the game would do and was already doing it. That makes the lever the
		/// choice and leaves no way to look without deciding, so the write waits for the player
		/// to say yes. The property is still what saves to PlayerPrefs, so taking it here is
		/// what remembers it between sittings.
		/// </summary>
		private void take()
		{
			if (GameFlow.Instance != null)
			{
				GameFlow.Instance.EasyMynas = DifficultyChoice.IsEasyAt(currentStop());
			}
		}

		/// <summary>
		/// Whether the panel may read the keyboard yet.
		///
		/// The Difficulty button is reached with the arrows and taken with Return - which is also
		/// the key that takes a difficulty and closes this panel. So the press that opens the panel
		/// is still down when the panel starts reading the keyboard, and the panel would shut on the
		/// press that opened it: the player hears the confirm, sees nothing, and the button looks
		/// dead. A mouse click never showed it, because a click raises no key.
		///
		/// This was first guarded by ignoring the single frame the panel opened on, which was not
		/// wide enough: a real press was logged opening the panel on one frame and closing it two
		/// frames later, because the EventSystem acts on a submit a frame before this component sees
		/// the key down. Rather than guess at a number of frames or a number of seconds, the panel
		/// waits to be armed by the key being released, and once armed it stays armed - a press
		/// holds the key down on the very frame it is reported, so asking for a free keyboard at
		/// that moment would refuse every real press. That is immune to any skew, and it also
		/// stops a held Return from opening and shutting the panel over and over.
		///
		/// The mirror of this is the one-frame delay on closing, which stops another reader on the
		/// screen taking the press that closes the panel.
		/// </summary>
		public static bool IsArmedNow(bool i_WasArmed, bool i_IsAnyCloseKeyHeld)
		{
			return i_WasArmed || !i_IsAnyCloseKeyHeld;
		}

		/// <summary>Whether a key that would close the panel is down right now.</summary>
		private bool isAnyCloseKeyHeld()
		{
			for (int key = 0; key < m_ChooseKeys.Length; ++key)
			{
				if (Input.GetKey(m_ChooseKeys[key]))
				{
					return true;
				}
			}

			for (int key = 0; key < m_CancelKeys.Length; ++key)
			{
				if (Input.GetKey(m_CancelKeys[key]))
				{
					return true;
				}
			}

			return false;
		}

		private void onSliderMoved(float i_Value)
		{
			int stop = DifficultyChoice.Clamp(Mathf.RoundToInt(i_Value));

			// The lever is the one control on this screen the arrows move without changing the
			// selection, so the navigation component that covers every other screen never hears
			// it. Unity raises this only when the value actually changes, and the slider counts
			// in whole numbers, so this is one click per stop rather than one per frame.
			UiSound.Play(m_Move, m_MoveVolume);

			// Moving the lever changes nothing but what the panel shows. The setting is written
			// by take(), when the player says yes, so a player can walk the ladder and look at
			// each stop without having chosen any of them.
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
			// Opening said something; putting it away should too, or the keystroke that closes
			// the panel is the only press on this screen that goes unanswered.
			UiSound.Play(m_Confirm);

			show(false);
			m_IsClosing = false;

			if (EventSystem.current != null && m_SelectedBefore != null)
			{
				EventSystem.current.SetSelectedGameObject(m_SelectedBefore);
				holdTheNavigationClick();
			}

			m_SelectedBefore = null;
		}

		/// <summary>
		/// Tells the screen's navigation click that the focus it is about to find somewhere new
		/// was moved by this panel, not by the player.
		///
		/// Opening raised a confirm and then handed the keyboard to the lever in the same frame,
		/// so the click meant for "you moved between buttons" landed on top of it. Two clips in
		/// one frame read as neither, which is why the Difficulty button sounded as though it
		/// had no sound of its own while Play, which moves no focus, sounded fine.
		///
		/// Found by component rather than wired, because <see cref="UiNavigationSound"/>'s own
		/// summary says to put it beside the screen's <see cref="FirstKeySelection"/> - which is
		/// this object. A screen without one simply has no click to hold.
		/// </summary>
		private void holdTheNavigationClick()
		{
			UiNavigationSound click = GetComponent<UiNavigationSound>();

			if (click != null)
			{
				click.Resync();
			}
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
