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

		private readonly Dictionary<Vector2Int, GameObject> r_PodVisuals = new Dictionary<Vector2Int, GameObject>();

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
		/// Draws one burst, fading it out over <see cref="m_BurstSeconds"/> and removing it.
		///
		/// A burst is built and thrown away each time. If profiling ever shows that this
		/// allocation matters, these pieces are the obvious thing to pool.
		/// </summary>
		private IEnumerator showBurst(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			int frameCount = m_Sprites.BurstFrameCount;

			if (frameCount <= 0 || i_Covered == null || i_Covered.Count == 0)
			{
				yield break;
			}

			GameObject root = new GameObject(string.Format("Burst_{0}_{1}", i_Origin.x, i_Origin.y));
			root.transform.SetParent(m_Container, false);

			List<BurstPiece> pieces = buildBurstPieces(root.transform, i_Origin, i_Covered);
			float secondsPerFrame = m_BurstSeconds / frameCount;

			for (int frame = 0; frame < frameCount; ++frame)
			{
				foreach (BurstPiece piece in pieces)
				{
					piece.Renderer.sprite = m_Sprites.GetBurst(piece.Piece, frame);
				}

				yield return new WaitForSeconds(secondsPerFrame);
			}

			Destroy(root);
		}

		private List<BurstPiece> buildBurstPieces(Transform i_Root, Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			List<BurstPiece> pieces = new List<BurstPiece>(i_Covered.Count);

			foreach (Vector2Int cell in i_Covered)
			{
				float angle;
				eBurstPiece piece = classifyPiece(cell, i_Origin, i_Covered, out angle);

				GameObject visual = createVisual(
					string.Format("Piece_{0}_{1}", cell.x, cell.y), cell, angle, m_BurstSortingOrder, i_Root);

				pieces.Add(new BurstPiece { Renderer = visual.GetComponent<SpriteRenderer>(), Piece = piece });
			}

			return pieces;
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

		private void clearVisuals()
		{
			r_PodVisuals.Clear();

			if (m_Container == null)
			{
				return;
			}

			for (int i = m_Container.childCount - 1; i >= 0; --i)
			{
				Destroy(m_Container.GetChild(i).gameObject);
			}
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
