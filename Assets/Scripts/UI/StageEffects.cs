using System.Collections;
using System.Collections.Generic;
using BomberBird.Arena;
using BomberBird.Enemies;
using BomberBird.Flow;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The three one-shot effects that say something just happened: dust as a pod lands, a
	/// puff of feathers where a myna fell, and a sparkle where the feather was collected.
	///
	/// It observes and nothing else, the same way <see cref="StageAudio"/> does. Each effect
	/// is drawn in answer to an event a gameplay component raised, so deleting this component
	/// takes the flourishes away and changes no rule of the game.
	///
	/// The burst is not one of these. <see cref="PodRenderer"/> draws it, because a burst
	/// covers many cells and each arm has to be rotated to point the right way. These three
	/// play on one cell, unrotated, which is the whole reason they share this component.
	/// </summary>
	[RequireComponent(typeof(BomberBird.Arena.Arena))]
	public class StageEffects : MonoBehaviour
	{
		/// <summary>
		/// One effect: the frames it fades over, how long that takes, and how high it draws.
		/// Grouped rather than left as nine loose fields, because all three are the same
		/// shape and the Inspector then shows each one as a named block.
		/// </summary>
		[System.Serializable]
		private class Effect
		{
			[Tooltip("Frames, strongest first. Played once and thrown away.")]
			public Sprite[] Frames;

			[Tooltip("How long the whole effect stays on screen.")]
			public float Seconds = 0.3f;

			[Tooltip("Sorting order. The arena is 0, a pod is 5, a myna is 9, the bird is 10, a burst is 20.")]
			public int SortingOrder = 21;
		}

		[Header("What it listens to")]
		[SerializeField] private ArenaPods m_Pods;
		[SerializeField] private ArenaMynas m_Mynas;
		[SerializeField] private StageObjective m_Objective;

		[Header("A myna is caught in a burst")]
		[Tooltip("Drawn over the burst that killed it, so the two read as separate things.")]
		[SerializeField] private Effect m_MynaPuff = new Effect();

		[Header("The bird picks up the freed feather")]
		[SerializeField] private Effect m_FeatherSparkle = new Effect();

		[Header("The bird drops a seed pod")]
		[Tooltip("Sorted under the pod on purpose: it must never hide the thing it announces.")]
		[SerializeField] private Effect m_PodDust = new Effect();

		[Header("Pooling")]
		[Tooltip("Effect visuals built before the stage runs. These three never overlap by more than a handful.")]
		[Min(0)]
		[SerializeField] private int m_PoolCapacity = 8;

		[Tooltip("Most visuals the pool will hold. One returned past this is destroyed instead of kept.")]
		[Min(1)]
		[SerializeField] private int m_PoolMaxSize = 32;

		// Every visual currently on loan. Membership is also the guard that makes a release
		// idempotent, so an effect cut short by OnDisable cannot be returned twice.
		private readonly HashSet<SpriteRenderer> r_LiveVisuals = new HashSet<SpriteRenderer>();

		private SpriteVisualPool m_Pool;
		private ArenaGrid m_Grid;
		private PodField m_Field;
		private Transform m_Container;

		private void Awake()
		{
			m_Grid = GetComponent<BomberBird.Arena.Arena>().Grid;

			if (m_Grid == null)
			{
				Debug.LogError(name + ": the Arena has no grid, so there is nowhere to draw.", this);
				enabled = false;
				return;
			}

			if (m_Pods != null)
			{
				m_Field = m_Pods.Field;
			}

			// One parent for everything drawn here, so clearing these never reaches the
			// arena's tiles or the pods.
			m_Container = new GameObject("StageEffects").transform;
			m_Container.SetParent(transform, false);

			m_Pool = new SpriteVisualPool(m_Container, "EffectVisuals", m_PoolCapacity, m_PoolMaxSize);

			reportUnassigned();
		}

		private void OnEnable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced += podField_PodPlaced;
			}

			if (m_Mynas != null)
			{
				m_Mynas.MynaDefeated += mynas_MynaDefeated;
			}

			if (m_Objective != null)
			{
				m_Objective.FeatherCollected += objective_FeatherCollected;
			}
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced -= podField_PodPlaced;
			}

			if (m_Mynas != null)
			{
				m_Mynas.MynaDefeated -= mynas_MynaDefeated;
			}

			if (m_Objective != null)
			{
				m_Objective.FeatherCollected -= objective_FeatherCollected;
			}

			// Coroutines stop on disable, so a half-played effect would otherwise be left
			// frozen on the arena.
			clearVisuals();
		}

		private void podField_PodPlaced(Vector2Int i_Cell)
		{
			play(m_PodDust, i_Cell);
		}

		private void mynas_MynaDefeated(Vector2Int i_Cell)
		{
			play(m_MynaPuff, i_Cell);
		}

		private void objective_FeatherCollected(Vector2Int i_Cell)
		{
			play(m_FeatherSparkle, i_Cell);
		}

		/// <summary>
		/// Starts one effect on one cell. An effect with no frames is silently skipped, so an
		/// unwired slot costs nothing at the moment it would have played - the warning for it
		/// was already given once, when the stage loaded.
		/// </summary>
		private void play(Effect i_Effect, Vector2Int i_Cell)
		{
			if (i_Effect.Frames == null || i_Effect.Frames.Length == 0)
			{
				return;
			}

			StartCoroutine(fade(i_Effect, i_Cell));
		}

		/// <summary>
		/// Draws the frames in order and gives the visual back.
		///
		/// Borrowed from <see cref="SpriteVisualPool"/> rather than built, the same way a
		/// burst piece is. The dust alone plays every time a pod is placed, which is often
		/// enough for the allocation to be worth not making.
		/// </summary>
		private IEnumerator fade(Effect i_Effect, Vector2Int i_Cell)
		{
			float secondsPerFrame = i_Effect.Seconds / i_Effect.Frames.Length;

			// Borrowed last, once nothing left in this method can fault: a visual taken and
			// then abandoned by a throw would sit out on loan until the next teardown.
			SpriteRenderer renderer = m_Pool.Get(
				m_Grid.CellToWorld(i_Cell), 0f, i_Effect.SortingOrder);

			r_LiveVisuals.Add(renderer);

			for (int frame = 0; frame < i_Effect.Frames.Length; ++frame)
			{
				renderer.sprite = i_Effect.Frames[frame];

				yield return new WaitForSeconds(secondsPerFrame);
			}

			release(renderer);
		}

		/// <summary>Returns one visual, once. A visual already given back is not in the set.</summary>
		private void release(SpriteRenderer i_Visual)
		{
			if (!r_LiveVisuals.Remove(i_Visual))
			{
				return;
			}

			m_Pool.Release(i_Visual);
		}

		/// <summary>
		/// Returns whatever is still on screen to the pool, so a disable inside a stage leaves
		/// the pool whole and an effect cut short mid-flight is swept up. It does not carry
		/// across stages: every stage and every retry is a fresh <c>LoadScene</c>, so the pool
		/// dies with the scene and the next one prewarms from nothing.
		/// </summary>
		private void clearVisuals()
		{
			if (m_Pool == null)
			{
				return;
			}

			// Copied, because releasing walks the set it is iterating.
			SpriteRenderer[] stranded = new SpriteRenderer[r_LiveVisuals.Count];
			r_LiveVisuals.CopyTo(stranded);

			for (int i = 0; i < stranded.Length; ++i)
			{
				release(stranded[i]);
			}
		}

		/// <summary>
		/// Says what is unwired and carries on. A missing publisher or an empty frame list is
		/// a wiring mistake worth seeing once in the console, but it is not a reason to drop
		/// the effects that were wired correctly.
		/// </summary>
		private void reportUnassigned()
		{
			string missing = "";

			if (m_Pods == null || m_Field == null)
			{
				missing += " pods";
			}

			if (m_Mynas == null)
			{
				missing += " mynas";
			}

			if (m_Objective == null)
			{
				missing += " objective";
			}

			if (m_MynaPuff.Frames == null || m_MynaPuff.Frames.Length == 0)
			{
				missing += " mynaPuff";
			}

			if (m_FeatherSparkle.Frames == null || m_FeatherSparkle.Frames.Length == 0)
			{
				missing += " featherSparkle";
			}

			if (m_PodDust.Frames == null || m_PodDust.Frames.Length == 0)
			{
				missing += " podDust";
			}

			if (missing.Length > 0)
			{
				Debug.LogWarning(name + ": these draw nothing, nothing is assigned:" + missing, this);
			}
		}
	}
}
