using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BomberBird.UI
{
	/// <summary>
	/// Feathers thrown in from both sides of the screen, then a few more drifting down for as
	/// long as the screen stays up. The closing screens' "the valley is yours again" beat, so the
	/// end of the campaign reads as a celebration rather than another card.
	///
	/// UI images moved by hand rather than a ParticleSystem, because the closing canvas is Screen
	/// Space - Overlay and particles do not draw over an Overlay canvas.
	///
	/// Armed on enable, so the panel it sits on decides when the burst happens, and a panel shown
	/// twice bursts twice. Thrown once the screen has faded in, not on enable itself: the scene
	/// loads under black, and on that first frame the canvas may not have sized this rect yet.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class FeatherConfetti : MonoBehaviour
	{
		private class Piece
		{
			public RectTransform Rect;
			public Vector2 Position;
			public Vector2 Velocity;
			public float Angle;
			public float Spin;
			public float SwayPhase;
		}

		[Tooltip("Every feather sprite a piece can be. One is picked at random for each.")]
		[SerializeField] private Sprite[] m_Feathers;

		[Header("The burst")]
		[Tooltip("Feathers thrown in from each side as the screen opens.")]
		[SerializeField] private int m_BurstPerSide = 22;

		[Tooltip("Launch speed range, in canvas pixels a second.")]
		[SerializeField] private Vector2 m_BurstSpeed = new Vector2(1500f, 2000f);

		[Tooltip("Launch angle range above the horizontal, in degrees, thrown towards the middle.")]
		[SerializeField] private Vector2 m_BurstAngle = new Vector2(50f, 75f);

		[Header("The drizzle")]
		[Tooltip("Feathers a second drifting down from the top once the burst is thrown.")]
		[SerializeField] private float m_DrizzlePerSecond = 5f;

		[Header("How a feather moves")]
		[Tooltip("Downward pull, in canvas pixels a second squared.")]
		[SerializeField] private float m_Gravity = 1400f;

		[Tooltip("How fast sideways speed dies away. A feather does not keep its throw for long.")]
		[SerializeField] private float m_Drag = 1f;

		[Tooltip("The fastest a feather falls. Low, because feathers float rather than drop.")]
		[SerializeField] private float m_FallSpeed = 170f;

		[Tooltip("Side to side drift while falling, in canvas pixels.")]
		[SerializeField] private float m_SwayWidth = 40f;

		[Tooltip("Size of one feather on the canvas, in pixels.")]
		[SerializeField] private float m_PieceSize = 88f;

		private const float k_LongestStep = 0.1f;

		private readonly List<Piece> r_Live = new List<Piece>();
		private readonly Stack<RectTransform> r_Spare = new Stack<RectTransform>();
		private RectTransform m_Area;
		private float m_DrizzleOwed;
		private bool m_IsBurstOwed;

		private void Awake()
		{
			m_Area = (RectTransform)transform;
		}

		private void OnEnable()
		{
			for (int i = r_Live.Count - 1; i >= 0; --i)
			{
				retire(i);
			}

			m_DrizzleOwed = 0f;
			m_IsBurstOwed = true;
		}

		private void Update()
		{
			if (m_Feathers == null || m_Feathers.Length == 0)
			{
				return;
			}

			// Nothing falls until the screen can be seen, so the burst is the first thing the
			// player sees rather than something already half over.
			if (m_IsBurstOwed)
			{
				if (!UiInput.IsListening() || m_Area.rect.width <= 0f)
				{
					return;
				}

				m_IsBurstOwed = false;
				burst(m_Area.rect.size * 0.5f);
			}

			// Unscaled, because the screen before this one may have left the clock stopped. Capped,
			// so a hitch - a scene load, an unfocused editor - drifts the feathers on rather than
			// flinging every one of them off the screen in a single frame.
			float deltaTime = Mathf.Min(Time.unscaledDeltaTime, k_LongestStep);
			float now = Time.unscaledTime;
			Vector2 half = m_Area.rect.size * 0.5f;

			m_DrizzleOwed += m_DrizzlePerSecond * deltaTime;

			while (m_DrizzleOwed >= 1f)
			{
				m_DrizzleOwed -= 1f;
				drizzle(half);
			}

			for (int i = r_Live.Count - 1; i >= 0; --i)
			{
				Piece piece = r_Live[i];

				piece.Velocity = Fall(piece.Velocity, deltaTime, m_Gravity, m_Drag, m_FallSpeed);
				piece.Position += piece.Velocity * deltaTime;

				if (piece.Position.y < -half.y - m_PieceSize)
				{
					retire(i);
					continue;
				}

				// The sway is drawn, not integrated, so it can never push a feather sideways
				// off the screen however long it falls.
				float sway = Mathf.Sin(now * 2.2f + piece.SwayPhase) * m_SwayWidth;

				piece.Angle += piece.Spin * deltaTime;
				piece.Rect.anchoredPosition = piece.Position + new Vector2(sway, 0f);
				piece.Rect.localRotation = Quaternion.Euler(0f, 0f, piece.Angle);
			}
		}

		/// <summary>
		/// One step of a feather's velocity. Gravity pulls it down to a slow float rather than a
		/// drop, and drag bleeds off the sideways throw without ever reversing it.
		/// </summary>
		public static Vector2 Fall(Vector2 i_Velocity, float i_DeltaTime, float i_Gravity, float i_Drag,
			float i_FallSpeed)
		{
			float x = i_Velocity.x * Mathf.Exp(-i_Drag * i_DeltaTime);
			float y = Mathf.Max(i_Velocity.y - i_Gravity * i_DeltaTime, -i_FallSpeed);

			// A feather already falling faster than the cap, which only happens if the cap was
			// lowered in the Inspector mid-flight, keeps its speed rather than being yanked up.
			if (i_Velocity.y < -i_FallSpeed)
			{
				y = i_Velocity.y;
			}

			return new Vector2(x, y);
		}

		private void burst(Vector2 i_Half)
		{
			for (int i = 0; i < m_BurstPerSide; ++i)
			{
				throwFromSide(-1f, i_Half);
				throwFromSide(1f, i_Half);
			}
		}

		/// <param name="i_Side">-1 for the left edge, 1 for the right.</param>
		private void throwFromSide(float i_Side, Vector2 i_Half)
		{
			float angle = Random.Range(m_BurstAngle.x, m_BurstAngle.y) * Mathf.Deg2Rad;
			float speed = Random.Range(m_BurstSpeed.x, m_BurstSpeed.y);

			// Thrown towards the middle, so the left side throws right and the right side left.
			Vector2 velocity = new Vector2(-i_Side * Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
			Vector2 start = new Vector2(i_Side * i_Half.x, Random.Range(-i_Half.y, -i_Half.y * 0.4f));

			spawn(start, velocity);
		}

		private void drizzle(Vector2 i_Half)
		{
			Vector2 start = new Vector2(Random.Range(-i_Half.x, i_Half.x), i_Half.y + m_PieceSize);

			spawn(start, new Vector2(0f, -m_FallSpeed));
		}

		private void spawn(Vector2 i_Position, Vector2 i_Velocity)
		{
			RectTransform rect = r_Spare.Count > 0 ? r_Spare.Pop() : makePiece();

			rect.gameObject.SetActive(true);
			rect.GetComponent<Image>().sprite = m_Feathers[Random.Range(0, m_Feathers.Length)];
			float angle = Random.Range(0f, 360f);

			rect.localRotation = Quaternion.Euler(0f, 0f, angle);
			rect.anchoredPosition = i_Position;

			r_Live.Add(new Piece
			{
				Rect = rect,
				Position = i_Position,
				Velocity = i_Velocity,
				Angle = angle,
				Spin = Random.Range(-180f, 180f),
				SwayPhase = Random.Range(0f, 2f * Mathf.PI),
			});
		}

		private RectTransform makePiece()
		{
			GameObject piece = new GameObject("Feather", typeof(RectTransform), typeof(Image));
			RectTransform rect = (RectTransform)piece.transform;

			rect.SetParent(m_Area, false);
			rect.sizeDelta = new Vector2(m_PieceSize, m_PieceSize);

			// Decoration never takes a click meant for the screen underneath.
			piece.GetComponent<Image>().raycastTarget = false;

			return rect;
		}

		private void retire(int i_Index)
		{
			RectTransform rect = r_Live[i_Index].Rect;

			rect.gameObject.SetActive(false);
			r_Spare.Push(rect);
			r_Live.RemoveAt(i_Index);
		}
	}
}
