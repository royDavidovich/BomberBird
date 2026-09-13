using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// Shows the right frame for what a bird is doing. Presentation only: it reads an
	/// <see cref="IGridWalker"/> and never decides anything about movement itself.
	///
	/// It asks for the interface rather than for <see cref="BirdMovement"/> so the mynas,
	/// which move by their own decisions rather than by input, are drawn by this same
	/// component instead of by a copy of it.
	/// </summary>
	[RequireComponent(typeof(SpriteRenderer))]
	public class BirdAnimator : MonoBehaviour
	{
		[SerializeField] private BirdSpriteSet m_Sprites;

		[Tooltip("Walk frames per second.")]
		[SerializeField] private float m_FrameRate = 8f;

		private SpriteRenderer m_Renderer;
		private IGridWalker m_Movement;
		private float m_StepTimer;
		private int m_Step;

		private void Awake()
		{
			m_Renderer = GetComponent<SpriteRenderer>();
			m_Movement = GetComponent<IGridWalker>();

			// RequireComponent cannot name an interface, so the check is made here instead.
			if (m_Movement == null)
			{
				Debug.LogError(name + ": no IGridWalker component to animate.", this);
				enabled = false;
				return;
			}

			if (m_Sprites == null)
			{
				Debug.LogError(name + ": m_Sprites is not assigned.", this);
				enabled = false;
				return;
			}

			string missing = m_Sprites.DescribeMissingSprites();

			if (missing != null)
			{
				Debug.LogError(name + ": sprite set '" + m_Sprites.name + "' is missing:" + missing, this);
				enabled = false;
			}
		}

		private void LateUpdate()
		{
			eFacing facing = m_Movement.Facing;

			if (m_Movement.IsMoving)
			{
				advanceWalkCycle();
				m_Renderer.sprite = m_Sprites.GetWalk(facing, m_Step);
			}
			else
			{
				// Restart the cycle so every walk begins on the same foot.
				m_StepTimer = 0f;
				m_Step = 0;
				m_Renderer.sprite = m_Sprites.GetIdle(facing);
			}

			m_Renderer.flipX = m_Sprites.IsMirrored(facing);
		}

		private void advanceWalkCycle()
		{
			if (m_FrameRate <= 0f)
			{
				return;
			}

			m_StepTimer += Time.deltaTime;

			float secondsPerFrame = 1f / m_FrameRate;

			while (m_StepTimer >= secondsPerFrame)
			{
				m_StepTimer -= secondsPerFrame;
				++m_Step;
			}
		}
	}
}
