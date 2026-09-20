using System.Collections.Generic;
using BomberBird.Arena;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// The burst-piece pool: that it hands the same instance back rather than building a new
	/// one, that nothing from a previous use survives into the next, and that it obeys the
	/// capacity and maximum it was given.
	///
	/// The reset cases are the point. A pooled sprite that keeps last frame's picture, last
	/// arm's rotation or last effect's sorting order is the classic pooling bug, and it looks
	/// like a rendering fault rather than a lifecycle one.
	/// </summary>
	public class SpriteVisualPoolTests
	{
		private readonly List<Object> r_Spawned = new List<Object>();

		[TearDown]
		public void TearDown()
		{
			for (int i = 0; i < r_Spawned.Count; ++i)
			{
				Object.DestroyImmediate(r_Spawned[i]);
			}

			r_Spawned.Clear();
		}

		[Test]
		public void ReusesAnInstanceInsteadOfBuildingANewOne()
		{
			SpriteVisualPool pool = makePool(1, 8);

			SpriteRenderer first = pool.Get(Vector3.zero, 0f, 0);
			int builtSoFar = pool.CountAll;

			pool.Release(first);

			SpriteRenderer second = pool.Get(Vector3.one, 90f, 3);

			Assert.AreSame(first, second, "A released visual must come back rather than a fresh one.");
			Assert.AreEqual(builtSoFar, pool.CountAll, "Reuse must not build another instance.");
		}

		[Test]
		public void ForgetsTheSpriteFromTheLastUse()
		{
			SpriteVisualPool pool = makePool(1, 8);

			SpriteRenderer visual = pool.Get(Vector3.zero, 0f, 0);
			visual.sprite = makeSprite();

			pool.Release(visual);

			Assert.IsNull(pool.Get(Vector3.zero, 0f, 0).sprite,
				"A reused visual must start blank, or it shows the last frame it drew.");
		}

		[Test]
		public void TakesItsPlacementFromTheCallerEveryTime()
		{
			SpriteVisualPool pool = makePool(1, 8);

			pool.Release(pool.Get(new Vector3(5f, 5f, 0f), 180f, 20));

			SpriteRenderer reused = pool.Get(new Vector3(1f, 2f, 0f), 90f, 7);

			Assert.AreEqual(new Vector3(1f, 2f, 0f), reused.transform.localPosition);
			Assert.AreEqual(90f, reused.transform.localEulerAngles.z, 0.001f);
			Assert.AreEqual(7, reused.sortingOrder);
		}

		[Test]
		public void HandsOutAnActiveVisual()
		{
			SpriteVisualPool pool = makePool(1, 8);

			Assert.IsTrue(pool.Get(Vector3.zero, 0f, 0).gameObject.activeSelf);
		}

		[Test]
		public void DeactivatesWhatItTakesBack()
		{
			SpriteVisualPool pool = makePool(1, 8);

			SpriteRenderer visual = pool.Get(Vector3.zero, 0f, 0);
			pool.Release(visual);

			Assert.IsFalse(visual.gameObject.activeSelf,
				"A returned visual left active would hang on the arena.");
		}

		[Test]
		public void PrewarmsItsInitialCapacityDeactivated()
		{
			// The lecturer's pool builds its instances during a loading or transition step,
			// not mid-gameplay. Prewarming is what makes the first burst of a stage free.
			SpriteVisualPool pool = makePool(4, 8);

			Assert.AreEqual(4, pool.CountAll, "Every instance of the initial capacity must exist up front.");
			Assert.AreEqual(4, pool.CountInactive);
			Assert.AreEqual(0, pool.CountActive);

			for (int i = 0; i < pool.Container.childCount; ++i)
			{
				Assert.IsFalse(pool.Container.GetChild(i).gameObject.activeSelf,
					"A prewarmed visual must be deactivated after it is created.");
			}
		}

		[Test]
		public void GrowsPastItsInitialCapacityWhenAskedForMore()
		{
			SpriteVisualPool pool = makePool(1, 8);

			pool.Get(Vector3.zero, 0f, 0);
			pool.Get(Vector3.zero, 0f, 0);
			pool.Get(Vector3.zero, 0f, 0);

			Assert.AreEqual(3, pool.CountActive);
			Assert.AreEqual(3, pool.CountAll, "The capacity is a starting point, not a ceiling.");
		}

		[Test]
		public void DestroysAReturnItCannotKeep()
		{
			SpriteVisualPool pool = makePool(0, 1);

			SpriteRenderer kept = pool.Get(Vector3.zero, 0f, 0);
			SpriteRenderer surplus = pool.Get(Vector3.zero, 0f, 0);

			pool.Release(kept);
			pool.Release(surplus);

			Assert.AreEqual(1, pool.CountInactive, "The maximum size must cap what is held.");
			Assert.IsTrue(surplus == null, "A return past the maximum must be destroyed, not leaked.");
		}

		[Test]
		public void RefusesTheSameVisualTwice()
		{
			SpriteVisualPool pool = makePool(1, 8);

			SpriteRenderer visual = pool.Get(Vector3.zero, 0f, 0);
			pool.Release(visual);

			Assert.Throws<System.InvalidOperationException>(() => pool.Release(visual),
				"A double release must be caught, not silently corrupt the pool.");
		}

		[Test]
		public void ClearDestroysEveryInstanceItIsHolding()
		{
			SpriteVisualPool pool = makePool(3, 8);

			pool.Clear();

			Assert.AreEqual(0, pool.CountAll);
			Assert.AreEqual(0, pool.Container.childCount);
		}

		private SpriteVisualPool makePool(int i_InitialCapacity, int i_MaxSize)
		{
			GameObject parent = new GameObject("PoolParent");
			r_Spawned.Add(parent);

			return new SpriteVisualPool(parent.transform, "Visuals", i_InitialCapacity, i_MaxSize);
		}

		private Sprite makeSprite()
		{
			Texture2D texture = new Texture2D(2, 2);
			r_Spawned.Add(texture);

			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
			r_Spawned.Add(sprite);

			return sprite;
		}
	}
}
