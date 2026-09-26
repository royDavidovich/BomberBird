using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// Darkens the whole screen around the cage and closes a spotlight onto it, with the cage
	/// rule written beside the lit circle.
	///
	/// Shown once a run, at the first cage stage, while the stage waits for Space
	/// (<see cref="CageHint"/> decides when). The hole starts wider than the screen and closes
	/// onto the cage, dipping just inside it before it settles, so the eye is pulled along with
	/// it; the words come in once it lands, and a thin ring on its edge breathes with the bars.
	/// When the stage starts it fades out rather than vanishing, so the arena comes back up
	/// under the player instead of cutting in.
	///
	/// The wash is one full-screen Image drawn with the UISpotlight shader. Its UVs run 0-1
	/// across the screen, the same space as a viewport point, so the cage's viewport position
	/// is the hole's centre with no conversion. Everything runs on unscaled time: the stage it
	/// sits over holds the clock at zero.
	/// </summary>
	public class CageSpotlight : MonoBehaviour
	{
		private static readonly int sr_Center = Shader.PropertyToID("_Center");
		private static readonly int sr_Aspect = Shader.PropertyToID("_Aspect");
		private static readonly int sr_Radius = Shader.PropertyToID("_Radius");
		private static readonly int sr_Softness = Shader.PropertyToID("_Softness");
		private static readonly int sr_RingWidth = Shader.PropertyToID("_RingWidth");
		private static readonly int sr_RingAlpha = Shader.PropertyToID("_RingAlpha");

		[Header("Parts")]
		[Tooltip("The full-screen wash, using a UISpotlight material. Stretched over the whole "
			+ "canvas so its UVs match the camera's viewport.")]
		[SerializeField] private Image m_Wash;

		[Tooltip("The words beside the hole. Moved to sit just above the cage, or below it when "
			+ "the cage is near the top of the screen.")]
		[SerializeField] private RectTransform m_Words;

		[Tooltip("Fades the words in and out together.")]
		[SerializeField] private CanvasGroup m_WordsGroup;

		[Header("The hole")]
		[Tooltip("Radius the hole settles at, in arena cells. A little over one, so the whole "
			+ "cage and a margin of floor around it are lit.")]
		[SerializeField] private float m_HoleCells = 1.3f;

		[Tooltip("Width of the soft edge from lit to dark, in arena cells.")]
		[SerializeField] private float m_SoftCells = 0.5f;

		[Tooltip("Radius the hole starts at, in screen heights. Over 1 starts it off the screen, "
			+ "so the first frame is the whole arena, lit.")]
		[SerializeField] private float m_StartRadius = 1.3f;

		[Tooltip("Seconds for the hole to close onto the cage.")]
		[SerializeField] private float m_CloseSeconds = 0.9f;

		[Tooltip("How far inside its final size the hole dips before settling, as a share of that "
			+ "size. The dip is what reads as the spotlight landing rather than stopping.")]
		[Range(0f, 0.5f)]
		[SerializeField] private float m_Overshoot = 0.2f;

		[Tooltip("How dark the screen outside the hole gets. 1 is black.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_Dim = 0.72f;

		[Header("The ring")]
		[Tooltip("Width of the ring on the hole's edge, in arena cells.")]
		[SerializeField] private float m_RingCells = 0.06f;

		[Tooltip("How bright the ring gets at the top of each breath.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_RingAlpha = 0.9f;

		[Tooltip("How bright it stays at the bottom of each breath, as a share of the top.")]
		[Range(0f, 1f)]
		[SerializeField] private float m_RingFloor = 0.35f;

		[Tooltip("Seconds for one breath. Matches the cage bars' pulse in CageHint.")]
		[SerializeField] private float m_PulseSeconds = 1.4f;

		[Header("Words and fades")]
		[Tooltip("Gap between the hole's edge and the words, in arena cells.")]
		[SerializeField] private float m_WordsGapCells = 0.4f;

		[Tooltip("Seconds for the words to come in once the hole has landed.")]
		[SerializeField] private float m_WordsInSeconds = 0.4f;

		[Tooltip("Seconds for everything to fade out once the stage starts.")]
		[SerializeField] private float m_FadeSeconds = 0.45f;

		private Material m_Material;
		private Vector2 m_Center;
		private float m_CellHeight;
		private float m_ShownAt;
		private float m_HideStartedAt = -1f;

		/// <summary>
		/// The hole's radius part way through closing. It eases in from the start radius, dips
		/// to <paramref name="i_Overshoot"/> inside the final radius over the last stretch, and
		/// ends exactly on it. Never reaches zero, so the cage is never fully dark.
		/// </summary>
		public static float IrisRadius(float i_Progress, float i_From, float i_To, float i_Overshoot)
		{
			float progress = Mathf.Clamp01(i_Progress);
			float remaining = 1f - progress;
			float closing = i_To + (i_From - i_To) * remaining * remaining * remaining;

			// The dip runs over the last 40% and is a single half sine, so it is zero at both of
			// its ends and the curve still lands on i_To.
			float dipProgress = Mathf.Clamp01((progress - 0.6f) / 0.4f);
			float dip = i_To * i_Overshoot * Mathf.Sin(dipProgress * Mathf.PI);

			return closing - dip;
		}

		private void OnDestroy()
		{
			if (m_Material != null)
			{
				Destroy(m_Material);
			}
		}

		/// <summary>Closes the spotlight onto a point in the world, seen through a camera.</summary>
		public void Show(Vector3 i_WorldPoint, Camera i_Camera)
		{
			if (m_Wash == null || i_Camera == null)
			{
				Debug.LogError(name + ": needs m_Wash and a camera to show the spotlight.", this);
				return;
			}

			// Made here rather than in Awake: this object is saved switched off, so Awake would
			// only run inside the SetActive below. An instance, so animating it never writes into
			// the material asset.
			if (m_Material == null)
			{
				m_Material = new Material(m_Wash.material);
				m_Wash.material = m_Material;
			}

			Vector3 viewport = i_Camera.WorldToViewportPoint(i_WorldPoint);

			m_Center = new Vector2(viewport.x, viewport.y);

			// One world unit is one arena cell, so this is a cell's height in screen heights.
			m_CellHeight = i_Camera.WorldToViewportPoint(i_WorldPoint + Vector3.up).y - viewport.y;

			gameObject.SetActive(true);
			placeWords();

			m_ShownAt = Time.unscaledTime;
			m_HideStartedAt = -1f;
			draw();
		}

		/// <summary>Fades the spotlight out. It switches itself off once the fade ends.</summary>
		public void Hide()
		{
			if (gameObject.activeSelf && m_HideStartedAt < 0f)
			{
				m_HideStartedAt = Time.unscaledTime;
			}
		}

		private void Update()
		{
			draw();
		}

		private void draw()
		{
			if (m_Material == null)
			{
				return;
			}

			float now = Time.unscaledTime;
			float sinceShown = now - m_ShownAt;
			float closing = m_CloseSeconds > 0f ? Mathf.Clamp01(sinceShown / m_CloseSeconds) : 1f;
			float sinceLanded = sinceShown - m_CloseSeconds;
			float fading = m_HideStartedAt < 0f ? 1f : 1f - Mathf.Clamp01((now - m_HideStartedAt) / Mathf.Max(m_FadeSeconds, 0.01f));

			if (fading <= 0f)
			{
				gameObject.SetActive(false);
				return;
			}

			RectTransform washRect = m_Wash.rectTransform;
			float targetRadius = m_HoleCells * m_CellHeight;

			m_Material.SetVector(sr_Center, m_Center);
			m_Material.SetFloat(sr_Aspect, washRect.rect.width / Mathf.Max(washRect.rect.height, 1f));
			m_Material.SetFloat(sr_Radius, IrisRadius(closing, m_StartRadius, targetRadius, m_Overshoot));
			m_Material.SetFloat(sr_Softness, m_SoftCells * m_CellHeight);
			m_Material.SetFloat(sr_RingWidth, m_RingCells * m_CellHeight);

			// The ring comes up as the hole lands, then breathes: brightest when the bars are
			// biggest, which is when UiPulse is at its floor.
			float ringIn = Mathf.Clamp01(sinceLanded / 0.25f);
			float breath = 1f - UiPulse.Alpha(now, m_PulseSeconds, 0f);
			m_Material.SetFloat(sr_RingAlpha, m_RingAlpha * ringIn * Mathf.Lerp(m_RingFloor, 1f, breath));

			// The wash darkens over the first half of the close, so the hole is already
			// visible by the time it is small enough to follow.
			float washIn = m_CloseSeconds > 0f ? Mathf.Clamp01(sinceShown / (m_CloseSeconds * 0.5f)) : 1f;
			Color wash = m_Wash.color;
			wash.a = m_Dim * washIn * fading;
			m_Wash.color = wash;

			if (m_WordsGroup != null)
			{
				float wordsIn = m_WordsInSeconds > 0f ? Mathf.Clamp01(sinceLanded / m_WordsInSeconds) : 1f;
				m_WordsGroup.alpha = wordsIn * fading;
			}
		}

		/// <summary>
		/// Anchors the words to the hole: above it, or below when above would run off the top,
		/// and slid sideways only as far as it takes to stay on screen.
		/// </summary>
		private void placeWords()
		{
			if (m_Words == null)
			{
				return;
			}

			Rect canvas = m_Wash.rectTransform.rect;
			float halfWidth = canvas.width > 0f ? m_Words.rect.width * 0.5f / canvas.width : 0f;
			float x = Mathf.Clamp(m_Center.x, halfWidth, 1f - halfWidth);
			float edge = (m_HoleCells + m_WordsGapCells) * m_CellHeight;
			bool isAbove = m_Center.y + edge + m_Words.rect.height / Mathf.Max(canvas.height, 1f) <= 1f;

			m_Words.anchorMin = new Vector2(x, m_Center.y);
			m_Words.anchorMax = m_Words.anchorMin;
			m_Words.pivot = new Vector2(0.5f, isAbove ? 0f : 1f);
			m_Words.anchoredPosition = new Vector2(0f, (isAbove ? edge : -edge) * canvas.height);
		}
	}
}
