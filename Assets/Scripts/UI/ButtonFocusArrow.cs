using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// Shows an arrow beside its button while that button has the player's attention.
	///
	/// The menu is reachable by mouse and by keyboard, and the two forms of attention overlap
	/// rather than replace each other: the player can hover Play, arrow-key across to Quit, and
	/// then move the mouse away. A single flag would leave an arrow stranded on the button the
	/// pointer has left, so the pointer and the selection are tracked apart and the arrow
	/// answers to either.
	/// </summary>
	public class ButtonFocusArrow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
		ISelectHandler, IDeselectHandler
	{
		[Header("Parts")]
		[Tooltip("Hidden until the button is hovered or selected.")]
		[SerializeField] private GameObject m_Arrow;

		private bool m_IsPointerInside;
		private bool m_IsSelected;

		public void OnPointerEnter(PointerEventData i_EventData)
		{
			m_IsPointerInside = true;
			refreshArrow();
		}

		public void OnPointerExit(PointerEventData i_EventData)
		{
			m_IsPointerInside = false;
			refreshArrow();
		}

		public void OnSelect(BaseEventData i_EventData)
		{
			m_IsSelected = true;
			refreshArrow();
		}

		public void OnDeselect(BaseEventData i_EventData)
		{
			m_IsSelected = false;
			refreshArrow();
		}

		private void OnDisable()
		{
			// A button hidden mid-hover never receives its exit, so it would come back wearing
			// an arrow it has not earned.
			m_IsPointerInside = false;
			m_IsSelected = false;
			refreshArrow();
		}

		private void refreshArrow()
		{
			if (m_Arrow != null)
			{
				m_Arrow.SetActive(m_IsPointerInside || m_IsSelected);
			}
		}
	}
}
