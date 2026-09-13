using BomberBird.Player;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// Hands the stage the bird the player chose: its frames, its speed, and the pod rules
	/// it plays by.
	///
	/// The scene is authored with the starter bird, so a gameplay scene opened on its own
	/// is still playable. This replaces those values when there is a run to ask.
	///
	/// It runs in Start rather than Awake on purpose. <see cref="PodField"/> is built during
	/// Awake by whichever consumer reaches it first, so Start is the earliest moment the
	/// rules are guaranteed to exist - and it is still a whole frame before the player can
	/// place anything.
	/// </summary>
	public class StageSetup : MonoBehaviour
	{
		[SerializeField] private BirdMovement m_Bird;
		[SerializeField] private BirdAnimator m_Animator;
		[SerializeField] private ArenaPods m_Pods;

		private void Start()
		{
			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			BirdProfile profile = GameFlow.Instance == null ? null : GameFlow.Instance.SelectedBird;

			if (profile == null)
			{
				// No run, or a run with no roster: the scene's own authored bird stands.
				return;
			}

			string problems = profile.DescribeProblems();

			if (problems != null)
			{
				Debug.LogError(
					name + ": bird profile '" + profile.name + "' is unusable, so the stage keeps "
					+ "the scene's bird:" + problems, this);
				return;
			}

			m_Bird.Speed = profile.Speed;
			m_Animator.SetSprites(profile.Sprites);

			PodField field = m_Pods.Field;

			if (field == null)
			{
				Debug.LogError(name + ": the ArenaPods component has no field to configure.", this);
				return;
			}

			field.BurstRange = profile.BurstRange;
			field.MaxActivePods = profile.MaxActivePods;
		}

		private bool hasRequiredReferences()
		{
			if (m_Bird == null)
			{
				Debug.LogError(name + ": m_Bird is not assigned.", this);
				return false;
			}

			if (m_Animator == null)
			{
				Debug.LogError(name + ": m_Animator is not assigned.", this);
				return false;
			}

			if (m_Pods == null)
			{
				Debug.LogError(name + ": m_Pods is not assigned.", this);
				return false;
			}

			return true;
		}
	}
}
