using System.Collections;
using System.Collections.Generic;
using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// Draws the seed pods and the bursts they become. Presentation only: it follows
	/// <see cref="PodField"/> through its events and decides nothing about the rules, so
	/// removing this component would leave placement, fuses, and chains exactly as they are.
	/// </summary>
	[RequireComponent(typeof(ArenaPods))]
	public class PodRenderer : MonoBehaviour
	{
		private const float k_MinPulseRate = 0.1f;

		private class BurstPiece
		{
			public SpriteRenderer Renderer;
			public eBurstPiece Piece;
		}

		[Header("Data")]
		[SerializeField] private PodSpriteSet m_Sprites;

		[Header("Rendering")]
		[Tooltip("Sorting order for a pod on the ground. Above the arena tiles, below the birds.")]
		[SerializeField] private int m_PodSortingOrder = 5;

		[Tooltip("Sorting order for a burst. Above the birds, because a burst is lethal and must be seen.")]
		[SerializeField] private int m_BurstSortingOrder = 20;

		[Header("Timing")]
		[Tooltip("Pulse frames per second the moment a pod is placed.")]
		[SerializeField] private float m_PulseStartRate = 4f;

		[Tooltip("Pulse frames per second just before the burst. Higher reads as more urgent.")]
		[SerializeField] private float m_PulseEndRate = 16f;

		[Tooltip("How long a burst stays on screen.")]
		[SerializeField] private float m_BurstSeconds = 0.45f;

		[Header("Pooling")]
		[Tooltip("Burst pieces built before the stage runs. A range 3 burst covers thirteen cells, so this holds several overlapping bursts without allocating.")]
		[Min(0)]
		[SerializeField] private int m_BurstPoolCapacity = 32;

		[Tooltip("Most pieces the pool will hold. A piece returned past this is destroyed instead of kept.")]
		[Min(1)]
		[SerializeField] private int m_BurstPoolMaxSize = 128;

		private readonly Dictionary<Vector2Int, GameObject> r_PodVisuals = new Dictionary<Vector2Int, GameObject>();

		// Every piece currently on loan from the pool. Membership is also the guard that makes
		// a release idempotent: a piece returned by the coroutine has already left the set, so
		// the teardown below cannot return it a second time.
		private readonly HashSet<SpriteRenderer> r_LivePieces = new HashSet<SpriteRenderer>();

		private SpriteVisualPool m_BurstPool;
		private ArenaPods m_Pods;
		private ArenaGrid m_Grid;
		private PodField m_Field;
		private Transform m_Container;

		private void Awake()
		{
			m_Pods = GetComponent<ArenaPods>();

			if (!hasRequiredReferences())
			{
				enabled = false;
				return;
			}

			m_Field = m_Pods.Field;
			m_Grid = GetComponent<BomberBird.Arena.Arena>().Grid;

			// One parent for everything drawn here, so the arena's tiles stay untouched
			// when these are cleared.
			m_Container = new GameObject("PodVisuals").transform;
			m_Container.SetParent(transform, false);

			m_BurstPool = new SpriteVisualPool(
				m_Container, "BurstPieces", m_BurstPoolCapacity, m_BurstPoolMaxSize);
		}

		private void OnEnable()
		{
			if (m_Field == null)
			{
				return;
			}

			m_Field.PodPlaced += podField_PodPlaced;
			m_Field.PodExploded += podField_PodExploded;
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced -= podField_PodPlaced;
				m_Field.PodExploded -= podField_PodExploded;
			}

			// Coroutines stop on disable, so anything half-drawn would otherwise be stranded.
			clearVisuals();
		}

		private void podField_PodPlaced(Vector2Int i_Cell)
		{
			StartCoroutine(pulsePod(i_Cell));
		}

		private void podField_PodExploded(Vector2Int i_Cell, IList<Vector2Int> i_Covered)
		{
			removePod(i_Cell);
			StartCoroutine(showBurst(i_Cell, i_Covered));
		}

		/// <summary>
		/// Pulses a placed pod until it bursts, speeding up as its fuse runs out. The clock
		/// is this coroutine's own: the pod's removal arrives as an event, so the two never
		/// need to agree on a remaining time.
		/// </summary>
		private IEnumerator pulsePod(Vector2Int i_Cell)
		{
			GameObject pod = createVisual(
				string.Format("Pod_{0}_{1}", i_Cell.x, i_Cell.y), i_Cell, 0f, m_PodSortingOrder, m_Container);

			SpriteRenderer renderer = pod.GetComponent<SpriteRenderer>();

			r_PodVisuals[i_Cell] = pod;

			float fuseSeconds = m_Pods.FuseSeconds;
			float elapsed = 0f;
			int step = 0;

			// Destroying the pod makes this null, which ends the loop without a second flag.
			while (pod != null)
			{
				renderer.sprite = m_Sprites.GetPodFrame(step);

				float progress = fuseSeconds <= 0f ? 1f : Mathf.Clamp01(elapsed / fuseSeconds);
				float rate = Mathf.Max(Mathf.Lerp(m_PulseStartRate, m_PulseEndRate, progress), k_MinPulseRate);
				float secondsPerFrame = 1f / rate;

				yield return new WaitForSeconds(secondsPerFrame);

				elapsed += secondsPerFrame;
				++step;
			}
		}

		/// <summary>
		/// Draws one burst, fading it out over <see cref="m_BurstSeconds"/> and giving its
		/// pieces back.
		///
		/// The pieces are borrowed from <see cref="SpriteVisualPool"/> rather than built:
		/// a burst lasts a fraction of a second and a chain sets off several at once, which
		/// is exactly the repeated create-and-retire the pool exists for.
		/// </summary>
		private IEnumerator showBurst(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			int frameCount = m_Sprites.BurstFrameCount;

			if (frameCount <= 0 || i_Covered == null || i_Covered.Count == 0)
			{
				yield break;
			}

			List<BurstPiece> pieces = buildBurstPieces(i_Origin, i_Covered);
			float secondsPerFrame = m_BurstSeconds / frameCount;

			for (int frame = 0; frame < frameCount; ++frame)
			{
				foreach (BurstPiece piece in pieces)
				{
					piece.Renderer.sprite = m_Sprites.GetBurst(piece.Piece, frame);
				}

				yield return new WaitForSeconds(secondsPerFrame);
			}

			foreach (BurstPiece piece in pieces)
			{
				releasePiece(piece.Renderer);
			}
		}

		private List<BurstPiece> buildBurstPieces(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			List<BurstPiece> pieces = new List<BurstPiece>(i_Covered.Count);

			foreach (Vector2Int cell in i_Covered)
			{
				float angle;
				eBurstPiece piece = classifyPiece(cell, i_Origin, i_Covered, out angle);

				SpriteRenderer renderer = m_BurstPool.Get(
					m_Grid.CellToWorld(cell), angle, m_BurstSortingOrder);

				r_LivePieces.Add(renderer);
				pieces.Add(new BurstPiece { Renderer = renderer, Piece = piece });
			}

			return pieces;
		}

		/// <summary>
		/// Returns one piece, once. A piece already given back is not in the set, so a burst
		/// cut short by <see cref="OnDisable"/> and then swept up cannot release it twice.
		/// </summary>
		private void releasePiece(SpriteRenderer i_Piece)
		{
			if (!r_LivePieces.Remove(i_Piece))
			{
				return;
			}

			m_BurstPool.Release(i_Piece);
		}

		/// <summary>
		/// Which drawing covers a cell, and how far it is rotated from the way it was drawn.
		///
		/// An arm's tip is simply its last covered cell, so an arm cut short by a wall or by
		/// a soft block still finishes with an end piece. Each direction is measured on its
		/// own, which is why a pod in a corner still reaches its full range the other way.
		/// </summary>
		private static eBurstPiece classifyPiece(
			Vector2Int i_Cell,
			Vector2Int i_Origin,
			IList<Vector2Int> i_Covered,
			out float o_Angle)
		{
			Vector2Int delta = i_Cell - i_Origin;

			if (delta == Vector2Int.zero)
			{
				o_Angle = 0f;

				return eBurstPiece.Centre;
			}

			Vector2Int direction = new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));

			// The pieces are drawn pointing right, so the rotation is the arm's own direction.
			o_Angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

			// A burst covers at most a handful of cells, so a scan costs less than a set.
			return i_Covered.Contains(i_Cell + direction) ? eBurstPiece.Arm : eBurstPiece.End;
		}

		private GameObject createVisual(
			string i_Name,
			Vector2Int i_Cell,
			float i_Angle,
			int i_SortingOrder,
			Transform i_Parent)
		{
			GameObject visual = new GameObject(i_Name);
			visual.transform.SetParent(i_Parent, false);
			visual.transform.localPosition = m_Grid.CellToWorld(i_Cell);
			visual.transform.localRotation = Quaternion.Euler(0f, 0f, i_Angle);

			SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
			renderer.sortingOrder = i_SortingOrder;

			return visual;
		}

		private void removePod(Vector2Int i_Cell)
		{
			GameObject pod;

			if (!r_PodVisuals.TryGetValue(i_Cell, out pod))
			{
				return;
			}

			r_PodVisuals.Remove(i_Cell);
			Destroy(pod);
		}

		/// <summary>
		/// Clears what is on screen. The burst pieces go back to the pool rather than to the
		/// garbage collector, so a disable inside a stage leaves the pool whole and a burst
		/// cut short mid-flight is swept up; only the pods, which are not pooled, are
		/// destroyed.
		///
		/// It does not carry across stages. Every stage and every retry is a fresh
		/// <c>LoadScene</c> (<see cref="BomberBird.Flow.GameFlow.StartStage"/>), so the pool
		/// dies with the scene and the next one prewarms from nothing.
		/// </summary>
		private void clearVisuals()
		{
			if (m_BurstPool != null)
			{
				// Copied, because releasing walks the set it is iterating.
				SpriteRenderer[] stranded = new SpriteRenderer[r_LivePieces.Count];
				r_LivePieces.CopyTo(stranded);

				for (int i = 0; i < stranded.Length; ++i)
				{
					releasePiece(stranded[i]);
				}
			}

			foreach (GameObject pod in r_PodVisuals.Values)
			{
				Destroy(pod);
			}

			r_PodVisuals.Clear();
		}

		private bool hasRequiredReferences()
		{
			if (m_Sprites == null)
			{
				Debug.LogError(name + ": m_Sprites is not assigned.", this);
				return false;
			}

			string missing = m_Sprites.DescribeMissingSprites();

			if (missing != null)
			{
				Debug.LogError(name + ": pod sprite set '" + m_Sprites.name + "' is missing: " + missing, this);
				return false;
			}

			if (m_Pods.Field == null)
			{
				Debug.LogError(name + ": the ArenaPods component has no field. Check the Arena's layout.", this);
				return false;
			}

			return true;
		}
	}
}
