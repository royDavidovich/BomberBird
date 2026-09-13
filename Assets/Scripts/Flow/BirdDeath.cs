using System.Collections;
using System.Collections.Generic;
using BomberBird.Player;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// Kills the bird when a burst catches it, then hands the death to <see cref="GameFlow"/>
	/// after a short beat so the player sees what hit them.
	///
	/// A burst stays lethal for as long as it is on screen, not only for the instant it goes
	/// off. Walking through a drawn burst unharmed would make failures unreadable, which is
	/// the one thing the design asks the player never to feel.
	/// </summary>
	[RequireComponent(typeof(BirdMovement))]
	public class BirdDeath : MonoBehaviour
	{
		[SerializeField] private ArenaPods m_Pods;

		[Header("Danger")]
		[Tooltip("Seconds a burst keeps killing. Keep this in step with the burst's time on screen.")]
		[SerializeField] private float m_LethalSeconds = 0.45f;

		[Header("Dying")]
		[Tooltip("Seconds the arena holds after a death before the stage reloads.")]
		[SerializeField] private float m_DeathBeat = 1f;

		[Tooltip("Flashes per second while the bird is dying.")]
		[SerializeField] private float m_FlashRate = 10f;

		private readonly Dictionary<Vector2Int, float> r_BurningUntil = new Dictionary<Vector2Int, float>();

		private BirdMovement m_Movement;
		private BirdPodPlacer m_Placer;
		private SpriteRenderer m_Renderer;
		private PodField m_Field;
		private bool m_IsDying;

		private void Awake()
		{
			m_Movement = GetComponent<BirdMovement>();
			m_Placer = GetComponent<BirdPodPlacer>();
			m_Renderer = GetComponent<SpriteRenderer>();

			if (m_Pods == null)
			{
				Debug.LogError(name + ": m_Pods is not assigned.", this);
				enabled = false;
				return;
			}

			m_Field = m_Pods.Field;

			if (m_Field == null)
			{
				Debug.LogError(name + ": the ArenaPods component has no field.", this);
				enabled = false;
			}
		}

		private void OnEnable()
		{
			if (m_Field != null)
			{
				m_Field.PodExploded += podField_PodExploded;
			}
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodExploded -= podField_PodExploded;
			}
		}

		private void Update()
		{
			if (m_IsDying)
			{
				return;
			}

			if (isBurning(m_Movement.Cell))
			{
				StartCoroutine(die());
			}
		}

		private void podField_PodExploded(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			float expiry = Time.time + m_LethalSeconds;

			foreach (Vector2Int cell in i_Covered)
			{
				// A later burst over the same cell extends the danger rather than shortening it.
				r_BurningUntil[cell] = expiry;
			}
		}

		private bool isBurning(Vector2Int i_Cell)
		{
			float expiry;

			return r_BurningUntil.TryGetValue(i_Cell, out expiry) && Time.time < expiry;
		}

		private IEnumerator die()
		{
			m_IsDying = true;

			// The bird is out of the player's hands from here.
			m_Movement.enabled = false;

			if (m_Placer != null)
			{
				m_Placer.enabled = false;
			}

			yield return flash();

			if (GameFlow.Instance == null)
			{
				Debug.LogError(name + ": no GameFlow in the scene, so the stage cannot reload.", this);
				yield break;
			}

			GameFlow.Instance.ReportDeath();
		}

		private IEnumerator flash()
		{
			if (m_Renderer == null || m_FlashRate <= 0f)
			{
				yield return new WaitForSeconds(m_DeathBeat);
				yield break;
			}

			float secondsPerFlash = 1f / m_FlashRate;
			float elapsed = 0f;

			while (elapsed < m_DeathBeat)
			{
				m_Renderer.enabled = !m_Renderer.enabled;

				yield return new WaitForSeconds(secondsPerFlash);

				elapsed += secondsPerFlash;
			}

			m_Renderer.enabled = true;
		}
	}
}
