using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Keeps this object only on a phone: the on-screen touch pad, and the buttons that stand
	/// in for a key a phone does not have. Mobile is not a declared platform (Docs/GDD.md
	/// section 8.3); these are an extra for the Web build played in a phone's browser.
	///
	/// The Device Simulator reports a phone too, which is how this is tried in the Editor.
	/// </summary>
	public class MobileOnly : MonoBehaviour
	{
		private void Awake()
		{
			gameObject.SetActive(Application.isMobilePlatform);
		}
	}
}
