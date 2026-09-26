using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace BomberBird.UI
{
	/// <summary>
	/// The touch pad's stick: a thumb landing anywhere on the zone this sits on brings the stick
	/// up under it, and lifting the thumb puts it back where it rests.
	///
	/// Unity's own OnScreenStick can only come up inside a circle around where it rests, which on a
	/// phone meant one corner of the screen, no room to pull towards the edge, and a thumb over
	/// the HUD. This is the same kind of control, reporting to the same gamepad stick, so the bird
	/// reads it exactly as it read that one.
	///
	/// Buttons drawn after the zone keep their own taps; the overlays drawn over the touch pad
	/// take every tap while they are up.
	/// </summary>
	public class FloatingStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
	{
		private const int k_NoPointer = int.MinValue;

		[InputControl(layout = "Vector2")]
		[SerializeField] private string m_ControlPath = "<Gamepad>/leftStick";

		[Tooltip("The ring and knob, moved together to where the thumb lands.")]
		[SerializeField] private RectTransform m_Stick;

		[Tooltip("Moved inside the ring by the drag.")]
		[SerializeField] private RectTransform m_Knob;

		[Tooltip("Fades the ring and knob between resting and held.")]
		[SerializeField] private CanvasGroup m_Group;

		[Tooltip("How far the knob travels from the centre for a full push, in canvas units.")]
		[SerializeField] private float m_Range = 90f;

		[Range(0f, 1f)]
		[SerializeField] private float m_RestAlpha = 0.35f;

		[Range(0f, 1f)]
		[SerializeField] private float m_HeldAlpha = 0.8f;

		private RectTransform m_Zone;
		private Vector2 m_RestPosition;
		private Vector2 m_Origin;
		private int m_PointerId = k_NoPointer;

		protected override string controlPathInternal
		{
			get { return m_ControlPath; }
			set { m_ControlPath = value; }
		}

		/// <summary>
		/// The stick value for a knob dragged <paramref name="i_Offset"/> from the centre: the
		/// offset as a share of the range, never more than a full push.
		/// </summary>
		public static Vector2 StickValue(Vector2 i_Offset, float i_Range)
		{
			if (i_Range <= 0f)
			{
				return Vector2.zero;
			}

			return Vector2.ClampMagnitude(i_Offset / i_Range, 1f);
		}

		private void Awake()
		{
			m_Zone = (RectTransform)transform;
			m_RestPosition = m_Stick.anchoredPosition;
			rest();
		}

		protected override void OnDisable()
		{
			// A pause or a stage ending mid-drag must not leave the bird walking on its own.
			release();

			base.OnDisable();
		}

		public void OnPointerDown(PointerEventData i_EventData)
		{
			if (m_PointerId != k_NoPointer)
			{
				return;
			}

			m_PointerId = i_EventData.pointerId;
			m_Origin = toZone(i_EventData);

			// Placed through the world position rather than the anchored one, so the stick can
			// rest anchored to the arena's corner and still land exactly under the thumb.
			Vector3 underThumb;
			RectTransformUtility.ScreenPointToWorldPointInRectangle(
				m_Zone, i_EventData.position, i_EventData.pressEventCamera, out underThumb);
			m_Stick.position = underThumb;
			m_Knob.anchoredPosition = Vector2.zero;
			m_Group.alpha = m_HeldAlpha;
		}

		public void OnDrag(PointerEventData i_EventData)
		{
			if (i_EventData.pointerId != m_PointerId)
			{
				return;
			}

			Vector2 offset = Vector2.ClampMagnitude(toZone(i_EventData) - m_Origin, m_Range);

			m_Knob.anchoredPosition = offset;
			SendValueToControl(StickValue(offset, m_Range));
		}

		public void OnPointerUp(PointerEventData i_EventData)
		{
			if (i_EventData.pointerId == m_PointerId)
			{
				release();
			}
		}

		private void release()
		{
			if (m_PointerId == k_NoPointer)
			{
				return;
			}

			m_PointerId = k_NoPointer;
			SendValueToControl(Vector2.zero);
			rest();
		}

		private void rest()
		{
			m_Stick.anchoredPosition = m_RestPosition;
			m_Knob.anchoredPosition = Vector2.zero;
			m_Group.alpha = m_RestAlpha;
		}

		private Vector2 toZone(PointerEventData i_EventData)
		{
			Vector2 local;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				m_Zone, i_EventData.position, i_EventData.pressEventCamera, out local);

			return local;
		}
	}
}
