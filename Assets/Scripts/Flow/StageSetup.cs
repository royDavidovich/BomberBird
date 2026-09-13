using BomberBird.Arena;
using BomberBird.Player;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.Flow
{
	/// <summary>
	/// Hands the stage everything the run decided: the arena it is played in, the habitat
	/// it is drawn with, and the bird flying it.
	///
	/// The scene is authored as the first stage with the starter bird, so a gameplay scene
	/// opened on its own is still playable. This replaces those values when there is a run
	/// to ask, and leaves them alone when there is not.
	///
	/// The two halves run in different callbacks because they are needed at different
	/// moments. The arena has to be chosen in Awake, before <see cref="Arena.Arena"/> builds
	/// its grid; the bird cannot be, because <see cref="PodField"/> is built during Awake by
	/// whichever consumer reaches it first, so Start is the earliest moment the pod rules are
	/// guaranteed to exist - and it is still a whole frame before the player can place
	/// anything.
	///
	/// The execution order is what makes the first half work: every Awake at the default
	/// order, the arena's included, runs after this one.
	/// </summary>
	[DefaultExecutionOrder(-100)]
	public class StageSetup : MonoBehaviour
	{
		[Header("The arena")]
		[SerializeField] private BomberBird.Arena.Arena m_Arena;
		[SerializeField] private ArenaRenderer m_Renderer;

		[Header("The bird")]
		[SerializeField] private BirdMovement m_Bird;
		[SerializeField] private BirdAnimator m_Animator;
		[SerializeField] private ArenaPods m_Pods;

		private void Awake()
		{
			Campaign.Stage stage = GameFlow.Instance == null ? null : GameFlow.Instance.CurrentStage;

			if (stage == null)
			{
				// No run, or a run past the end of the campaign: the scene's own arena stands.
				return;
			}

			if (m_Arena == null || m_Renderer == null)
			{
				Debug.LogError(
					name + ": the arena references are not assigned, so the stage keeps the "
					+ "scene's arena.", this);
				return;
			}

			// A stage missing either is already reported by Campaign.DescribeProblems, so
			// keeping the scene's own is the recovery rather than a second complaint.
			if (stage.Layout != null)
			{
				m_Arena.UseLayout(stage.Layout);
			}

			if (stage.TileSet != null)
			{
				m_Renderer.UseTileSet(stage.TileSet);
			}
		}

		private void Start()
		{
			if (!hasRequiredBirdReferences())
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

		private bool hasRequiredBirdReferences()
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
