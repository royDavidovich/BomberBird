using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// Places a seed pod under the bird when the player asks for one.
	///
	/// Input lives with the player, the way movement input does. Whether a pod may actually
	/// be placed is <see cref="PodField"/>'s decision, not this component's.
	/// </summary>
	[RequireComponent(typeof(BirdMovement))]
	public class BirdPodPlacer : MonoBehaviour
	{
		// The built-in Input Manager maps "Jump" to Space and to a gamepad face button,
		// which is exactly the pair Docs/GDD.md asks for.
		private const string k_PlaceButton = "Jump";

		[SerializeField] private ArenaPods m_Pods;

		private BirdMovement m_Movement;

		private void Awake()
		{
			m_Movement = GetComponent<BirdMovement>();

			if (m_Pods == null)
			{
				Debug.LogError(name + ": m_Pods is not assigned.", this);
				enabled = false;
			}
		}

		private void Update()
		{
			PodField field = m_Pods.Field;

			if (field == null)
			{
				return;
			}

			Vector2Int cell = m_Movement.Cell;

			// Every cell except this one has been stepped off, so a pod waiting there turns
			// solid. Checked every frame, because the bird slides between cells continuously.
			field.MarkVacatedExcept(cell);

			// Input belongs in Update, never in FixedUpdate.
			if (Input.GetButtonDown(k_PlaceButton))
			{
				field.TryPlace(cell);
			}
		}
	}
}
