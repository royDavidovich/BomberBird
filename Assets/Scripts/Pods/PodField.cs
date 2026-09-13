using System;
using System.Collections.Generic;
using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// Every seed pod currently on the arena, and the rules that govern them: placement,
	/// fuses, chain reactions, and what a burst destroys.
	///
	/// Plain C# with no scene and no MonoBehaviour, so the rules can be stepped through a
	/// whole detonation in a test without entering Play Mode.
	/// </summary>
	public class PodField
	{
		private class Pod
		{
			public Vector2Int Cell;
			public float FuseRemaining;
			public bool IsSolid;        // false while the placer is still standing on it
			public bool IsSpent;
		}

		private readonly ArenaGrid r_Grid;
		private readonly List<Pod> r_Pods = new List<Pod>();

		private readonly float r_FuseSeconds;

		// Not readonly: the bird being played sets these, and the player may change bird
		// between stages. Both are clamped to at least 1, because a burst that reaches
		// nowhere and a limit of no pods are each an unplayable arena rather than a rule.
		private int m_BurstRange;
		private int m_MaxActivePods;

		/// <summary>Raised when a pod is placed, with its cell.</summary>
		public event Action<Vector2Int> PodPlaced;

		/// <summary>Raised when a pod detonates, with every cell its burst covers.</summary>
		public event Action<Vector2Int, IList<Vector2Int>> PodExploded;

		public int ActivePodCount
		{
			get { return r_Pods.Count; }
		}

		/// <summary>How many cells a burst reaches along each of the four directions.</summary>
		public int BurstRange
		{
			get { return m_BurstRange; }
			set { m_BurstRange = Math.Max(1, value); }
		}

		/// <summary>How many pods may sit on the arena at once.</summary>
		public int MaxActivePods
		{
			get { return m_MaxActivePods; }
			set { m_MaxActivePods = Math.Max(1, value); }
		}

		public PodField(ArenaGrid i_Grid, float i_FuseSeconds, int i_BurstRange, int i_MaxActivePods)
		{
			if (i_Grid == null)
			{
				throw new ArgumentNullException("i_Grid");
			}

			r_Grid = i_Grid;
			r_FuseSeconds = i_FuseSeconds;
			BurstRange = i_BurstRange;
			MaxActivePods = i_MaxActivePods;
		}

		/// <summary>
		/// Places a pod. Fails when the limit is reached, when the cell already holds one,
		/// or when the cell is not open floor.
		/// </summary>
		public bool TryPlace(Vector2Int i_Cell)
		{
			if (r_Pods.Count >= m_MaxActivePods)
			{
				return false;
			}

			if (!r_Grid.IsWalkable(i_Cell) || HasPodAt(i_Cell))
			{
				return false;
			}

			// Not solid yet: the bird that placed it is standing on it and must be able to
			// step off. It becomes solid once vacated.
			r_Pods.Add(new Pod { Cell = i_Cell, FuseRemaining = r_FuseSeconds, IsSolid = false });
			OnPodPlaced(i_Cell);

			return true;
		}

		public bool HasPodAt(Vector2Int i_Cell)
		{
			return findPod(i_Cell) != null;
		}

		/// <summary>
		/// True when a pod in this cell should stop a bird. A pod the bird has not yet
		/// stepped off does not block it; once it has left, the pod is solid and it cannot
		/// step back on.
		/// </summary>
		public bool IsBlocking(Vector2Int i_Cell)
		{
			Pod pod = findPod(i_Cell);

			return pod != null && pod.IsSolid;
		}

		/// <summary>
		/// Tells the field where the bird's body is. Every pod that body no longer covers
		/// turns solid. Single-player, so one bird is the whole story. Safe to call every
		/// frame.
		///
		/// The body, not the cell its centre falls in: stepping off a pod is continuous, and
		/// the centre reaches the next cell while the body still overlaps the one behind it.
		/// A pod that turned solid then would wall the bird in where it stands.
		/// </summary>
		public void MarkVacatedExcept(Vector2 i_BodyCentre, float i_BodyHalfExtent)
		{
			foreach (Pod pod in r_Pods)
			{
				if (!covers(i_BodyCentre, i_BodyHalfExtent, pod.Cell))
				{
					pod.IsSolid = true;
				}
			}
		}

		/// <summary>
		/// Advances every fuse. Any pod that reaches zero detonates, and its burst sets off
		/// any pod it covers, so a chain resolves inside a single call.
		/// </summary>
		public void Tick(float i_DeltaSeconds)
		{
			List<Pod> due = null;

			foreach (Pod pod in r_Pods)
			{
				pod.FuseRemaining -= i_DeltaSeconds;

				if (pod.FuseRemaining <= 0f)
				{
					if (due == null)
					{
						due = new List<Pod>();
					}

					due.Add(pod);
				}
			}

			if (due == null)
			{
				return;
			}

			foreach (Pod pod in due)
			{
				detonate(pod);
			}
		}

		/// <summary>Detonates the pod in this cell immediately, if there is one.</summary>
		public bool TryDetonateAt(Vector2Int i_Cell)
		{
			Pod pod = findPod(i_Cell);

			if (pod == null)
			{
				return false;
			}

			detonate(pod);

			return true;
		}

		private void detonate(Pod i_Pod)
		{
			if (i_Pod.IsSpent)
			{
				// Already gone: reached by a chain while its own fuse was also due.
				return;
			}

			i_Pod.IsSpent = true;
			r_Pods.Remove(i_Pod);

			List<Vector2Int> covered = BurstShape.GetCoveredCells(r_Grid, i_Pod.Cell, m_BurstRange);

			// Destroy first, so a listener drawing the burst sees the arena as it now is.
			foreach (Vector2Int cell in covered)
			{
				r_Grid.TryDestroy(cell);
			}

			OnPodExploded(i_Pod.Cell, covered);

			// Chain: anything the burst reached goes off too. Collected first so the list
			// is not modified while it is being walked.
			List<Pod> caught = null;

			foreach (Vector2Int cell in covered)
			{
				Pod other = findPod(cell);

				if (other == null || other.IsSpent)
				{
					continue;
				}

				if (caught == null)
				{
					caught = new List<Pod>();
				}

				caught.Add(other);
			}

			if (caught == null)
			{
				return;
			}

			foreach (Pod pod in caught)
			{
				detonate(pod);
			}
		}

		/// <summary>
		/// Whether a body of this size, centred here, covers any part of this cell. Asks the
		/// grid the same four corners <see cref="ArenaGrid.IsAreaWalkable"/> asks, so the
		/// moment a pod stops being stood on and the moment it starts blocking cannot drift
		/// apart.
		/// </summary>
		private bool covers(Vector2 i_BodyCentre, float i_BodyHalfExtent, Vector2Int i_Cell)
		{
			float left = i_BodyCentre.x - i_BodyHalfExtent;
			float right = i_BodyCentre.x + i_BodyHalfExtent;
			float bottom = i_BodyCentre.y - i_BodyHalfExtent;
			float top = i_BodyCentre.y + i_BodyHalfExtent;

			return r_Grid.WorldToCell(new Vector3(left, bottom, 0f)) == i_Cell
				|| r_Grid.WorldToCell(new Vector3(right, bottom, 0f)) == i_Cell
				|| r_Grid.WorldToCell(new Vector3(left, top, 0f)) == i_Cell
				|| r_Grid.WorldToCell(new Vector3(right, top, 0f)) == i_Cell;
		}

		private Pod findPod(Vector2Int i_Cell)
		{
			foreach (Pod pod in r_Pods)
			{
				if (pod.Cell == i_Cell)
				{
					return pod;
				}
			}

			return null;
		}

		private void OnPodPlaced(Vector2Int i_Cell)
		{
			Action<Vector2Int> handler = PodPlaced;

			if (handler != null)
			{
				handler(i_Cell);
			}
		}

		private void OnPodExploded(Vector2Int i_Cell, IList<Vector2Int> i_Covered)
		{
			Action<Vector2Int, IList<Vector2Int>> handler = PodExploded;

			if (handler != null)
			{
				handler(i_Cell, i_Covered);
			}
		}
	}
}
