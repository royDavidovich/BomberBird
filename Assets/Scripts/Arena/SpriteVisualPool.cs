using UnityEngine;
using UnityEngine.Pool;

namespace BomberBird.Arena
{
	/// <summary>
	/// Reuses the throwaway sprites the arena draws on itself: the pieces of a burst and the
	/// one-shot dust, feather and sparkle effects.
	///
	/// Pooling earns its place here and almost nowhere else in this project. Every one of
	/// these is created the moment something happens, drawn for a fraction of a second and
	/// thrown away, and a chain reaction does it many times over in the same frame - a range
	/// 3 burst alone covers thirteen cells, so a four-pod chain is more than fifty sprites
	/// built and destroyed while the player is moving. That is the repeated create-and-retire
	/// shape a pool is for. A pod visual, by contrast, appears once per placement and is left
	/// on <c>Instantiate</c>.
	///
	/// The lifecycle is the whole contract, and it runs in this order:
	///
	/// <list type="table">
	/// <item><term>Create</term><description>Build the GameObject and its
	/// <see cref="SpriteRenderer"/> once, under this pool's own container.</description></item>
	/// <item><term>Get</term><description>Clear the sprite left by the last use, place and
	/// rotate the visual where the caller asked, set its sorting order, then activate
	/// it.</description></item>
	/// <item><term>Release</term><description>Clear the sprite and deactivate. Nothing is
	/// destroyed, so <c>OnDestroy</c> never runs and there is no teardown to put
	/// there.</description></item>
	/// <item><term>Destroy</term><description>Only for a return the pool cannot keep, once it
	/// is already holding its maximum.</description></item>
	/// </list>
	///
	/// The initial capacity is built up front and deactivated, so a stage's first burst costs
	/// no allocation, and the pool still grows if a chain asks for more than was expected.
	/// </summary>
	public class SpriteVisualPool
	{
		private readonly ObjectPool<SpriteRenderer> r_Pool;
		private readonly Transform r_Container;

		public SpriteVisualPool(Transform i_Parent, string i_Name, int i_InitialCapacity, int i_MaxSize)
		{
			// Its own container, so clearing the pool never reaches whatever else the caller
			// parents to the same transform.
			r_Container = new GameObject(i_Name).transform;
			r_Container.SetParent(i_Parent, false);

			int maxSize = Mathf.Max(1, i_MaxSize);
			int capacity = Mathf.Clamp(i_InitialCapacity, 0, maxSize);

			r_Pool = new ObjectPool<SpriteRenderer>(
				createVisual,
				null,
				releaseVisual,
				destroyVisual,
				true,
				Mathf.Max(1, capacity),
				maxSize);

			prewarm(capacity);
		}

		/// <summary>Where the pooled visuals live. Everything under it belongs to this pool.</summary>
		public Transform Container
		{
			get { return r_Container; }
		}

		public int CountAll
		{
			get { return r_Pool.CountAll; }
		}

		public int CountActive
		{
			get { return r_Pool.CountActive; }
		}

		public int CountInactive
		{
			get { return r_Pool.CountInactive; }
		}

		/// <summary>
		/// Takes a blank visual, places it and switches it on. The caller sets the sprite -
		/// this decides nothing about what is drawn, only that whatever is drawn starts clean.
		/// </summary>
		public SpriteRenderer Get(Vector3 i_LocalPosition, float i_Angle, int i_SortingOrder)
		{
			SpriteRenderer visual = r_Pool.Get();

			visual.transform.localPosition = i_LocalPosition;
			visual.transform.localRotation = Quaternion.Euler(0f, 0f, i_Angle);
			visual.sortingOrder = i_SortingOrder;
			visual.gameObject.SetActive(true);

			return visual;
		}

		/// <summary>
		/// Gives a visual back. Returning the same one twice throws rather than quietly
		/// handing one instance to two callers, which is the bug that would follow.
		/// </summary>
		public void Release(SpriteRenderer i_Visual)
		{
			if (i_Visual == null)
			{
				return;
			}

			r_Pool.Release(i_Visual);
		}

		/// <summary>
		/// Destroys everything the pool is holding. For a teardown where the visuals go with
		/// the stage; anything still out on loan must be released first.
		/// </summary>
		public void Clear()
		{
			r_Pool.Clear();
		}

		private SpriteRenderer createVisual()
		{
			GameObject visual = new GameObject(r_Container.name + "_Piece");
			visual.transform.SetParent(r_Container, false);
			visual.SetActive(false);

			return visual.AddComponent<SpriteRenderer>();
		}

		private static void releaseVisual(SpriteRenderer i_Visual)
		{
			i_Visual.sprite = null;
			i_Visual.gameObject.SetActive(false);
		}

		private static void destroyVisual(SpriteRenderer i_Visual)
		{
			if (i_Visual == null)
			{
				return;
			}

			// Destroy commits at the end of the frame and is refused outside play mode, which
			// the edit-mode tests for this class run in.
			if (Application.isPlaying)
			{
				Object.Destroy(i_Visual.gameObject);
			}
			else
			{
				Object.DestroyImmediate(i_Visual.gameObject);
			}
		}

		/// <summary>
		/// Builds the initial capacity and hands it straight back, so every one of those
		/// instances exists and is deactivated before the first burst asks for one.
		/// </summary>
		private void prewarm(int i_Count)
		{
			if (i_Count <= 0)
			{
				return;
			}

			SpriteRenderer[] warmed = new SpriteRenderer[i_Count];

			for (int i = 0; i < i_Count; ++i)
			{
				warmed[i] = r_Pool.Get();
			}

			for (int i = 0; i < i_Count; ++i)
			{
				r_Pool.Release(warmed[i]);
			}
		}
	}
}
