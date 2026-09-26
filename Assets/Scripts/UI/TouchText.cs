using TMPro;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// Rewrites a keyboard prompt for a phone, where "press Space" names a key that is not
	/// there. The desktop text stays in the label as authored.
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	public class TouchText : MonoBehaviour
	{
		[Tooltip("Shown instead of the label's own text on a phone.")]
		[TextArea]
		[SerializeField] private string m_TouchText;

		private void Awake()
		{
			if (Application.isMobilePlatform && !string.IsNullOrEmpty(m_TouchText))
			{
				GetComponent<TMP_Text>().text = m_TouchText;
			}
		}
	}
}
