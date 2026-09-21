using UnityEngine;
using UnityEngine.EventSystems;

namespace BomberBird.UI
{
	/// <summary>
	/// Shows an arrow beside its button while that button is the one selected.
	///
	/// The arrow used to answer to the pointer as well as the selection, so that a hovered
	/// button wore one too. That put **two** arrows on the menu whenever the mouse happened to
	/// be resting over one button while the keyboard had moved to another - the menu read as
	/// broken, and neither arrow was telling the truth about what Return would press.
	///
	/// There is exactly one selection, so there is now exactly one arrow. The mouse has not lost
	/// anything: clicking a button selects it, so the arrow follows a click, and a button under
	/// the pointer still lights through its own Button transition.
	/// </summary>
	public class ButtonFocusArrow : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		[Header("Parts")]
		[Tooltip("Hidden until the button is the selected one.")]
		[SerializeField] private GameObject m_Arrow;

		private bool m_IsSelected;

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
			// A button hidden while selected never receives its deselect, so it would come back
			// wearing an arrow it has not earned.
			m_IsSelected = false;
			refreshArrow();
		}

		private void refreshArrow()
		{
			if (m_Arrow != null)
			{
				m_Arrow.SetActive(m_IsSelected);
			}
		}
	}
}
