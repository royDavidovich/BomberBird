using BomberBird.Flow;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// How the game is played, over the arena it is played in.
	///
	/// Shown once a run, as the intro stage opens, and again whenever the player asks for it
	/// from the pause overlay. One panel serving both, rather than a screen before the game
	/// and a copy inside it: two copies of the same rules drift apart, and the one that drifts
	/// is always the one nobody is looking at.
	///
	/// It is shown over the loaded arena on purpose. A player reading "space places a seed
	/// pod" with the pods, the blocks and the mynas already on screen behind the panel can see
	/// what every word refers to.
	///
	/// This panel is the topmost thing in the canvas, and the two components that also read
	/// the keyboard - <see cref="PauseController"/> and <see cref="StageReady"/> - both stand
	/// down while <see cref="IsShown"/> is true. That is the same layering the overlays
	/// already use, rather than a new input owner.
	/// </summary>
	public class InstructionsPanel : MonoBehaviour
	{
		[Header("Parts")]
		[Tooltip("The panel itself. Hidden at once on a run that has already seen it.")]
		[SerializeField] private GameObject m_Panel;

		[Tooltip("Hidden until the player has had a moment to read, like the closing screens.")]
		[SerializeField] private GameObject m_ContinueHint;

		[Header("Timing")]
		[Tooltip("Seconds before a press is accepted. This is the longest text in the game "
			+ "and it arrives while the player is still pressing Play.")]
		[SerializeField] private float m_Hold = 1.2f;

		private float m_ShownAt;
		private bool m_IsClosing;

		/// <summary>
		/// Whether the panel is up and owns the keyboard. Read by the pause overlay and by
		/// the stage-ready hold, so a keystroke that dismisses this cannot also unpause the
		/// game or start the stage behind it.
		/// </summary>
		public bool IsShown
		{
			get { return m_Panel != null && m_Panel.activeSelf; }
		}

		private void Awake()
		{
			// Hidden in Awake rather than trusted to be hidden in the scene, because the
			// panel is worked on with it open and would otherwise ship that way.
			show(false);
		}

		private void Start()
		{
			// Start, not Awake: on the first load of a run, GameFlow's own Awake is what
			// builds the run this asks.
			if (GameFlow.Instance != null && GameFlow.Instance.ClaimInstructionsShowing())
			{
				Show();
			}
		}

		/// <summary>Opens the panel. Wired to the pause overlay's Help button.</summary>
		public void Show()
		{
			show(true);

			m_IsClosing = false;
			m_ShownAt = Time.unscaledTime;
		}

		private void Update()
		{
			if (m_IsClosing)
			{
				// One frame later than the press, and the reason is the whole layering. The
				// order Unity runs Update in is undefined, so closing on the press itself
				// would let PauseController or StageReady see IsShown already false and take
				// the same keystroke - unpausing the game, or starting the stage, under a
				// player who only meant to put the rules away. Staying up for the rest of
				// the frame means both of them stand down whichever order they run in.
				show(false);
				m_IsClosing = false;
				return;
			}

			if (!IsShown || Time.unscaledTime - m_ShownAt < m_Hold)
			{
				return;
			}

			if (m_ContinueHint != null && !m_ContinueHint.activeSelf)
			{
				m_ContinueHint.SetActive(true);
			}

			if (UiInput.WasKeyPressed())
			{
				m_IsClosing = true;
			}
		}

		private void show(bool i_IsShown)
		{
			if (m_Panel != null)
			{
				m_Panel.SetActive(i_IsShown);
			}

			if (m_ContinueHint != null)
			{
				m_ContinueHint.SetActive(false);
			}
		}
	}
}
