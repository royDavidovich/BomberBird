using BomberBird.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BomberBird.Tests
{
	/// <summary>
	/// Every slider in the game can be taken by a finger. A Slider only hears a press on a
	/// raycast graphic under it, and these only had their small handle, which a thumb on a phone
	/// almost never lands on: the difficulty lever could not be moved at all. Each now carries an
	/// invisible TouchArea wider and taller than its bar.
	/// </summary>
	public class SliderTouchAreaTests
	{
		private const float k_MinExtraHeight = 10f;

		private Scene m_Scene;
		private bool m_IsOpenedHere;

		[TearDown]
		public void TearDown()
		{
			if (m_IsOpenedHere && m_Scene.isLoaded)
			{
				EditorSceneManager.CloseScene(m_Scene, true);
			}
		}

		[Test]
		public void TheDifficultyLeverTakesAFinger()
		{
			open("Assets/Scenes/MainMenu.unity");
			DifficultyPanel panel = find<DifficultyPanel>();
			Slider slider = new SerializedObject(panel).FindProperty("m_Slider").objectReferenceValue as Slider;

			assertTouchArea(slider);
		}

		[Test]
		public void ThePauseVolumeSlidersTakeAFinger()
		{
			open("Assets/Scenes/Gameplay.unity");
			PauseController pause = find<PauseController>();
			SerializedObject wiring = new SerializedObject(pause);

			assertTouchArea(wiring.FindProperty("m_MusicSlider").objectReferenceValue as Slider);
			assertTouchArea(wiring.FindProperty("m_EffectsSlider").objectReferenceValue as Slider);
		}

		private static void assertTouchArea(Slider i_Slider)
		{
			Assert.IsNotNull(i_Slider, "The slider is not wired.");

			Transform area = i_Slider.transform.Find("TouchArea");
			Assert.IsNotNull(area, i_Slider.name + " has no TouchArea.");
			Assert.AreEqual(0, area.GetSiblingIndex(), "The TouchArea must draw behind the bar.");

			Image image = area.GetComponent<Image>();
			Assert.IsTrue(image.raycastTarget, i_Slider.name + ": the TouchArea must take the press.");
			Assert.AreEqual(0f, image.color.a, i_Slider.name + ": the TouchArea must stay invisible.");

			RectTransform areaRect = (RectTransform)area;
			Assert.GreaterOrEqual(areaRect.offsetMax.y - areaRect.offsetMin.y, k_MinExtraHeight, i_Slider.name);
			Assert.Greater(areaRect.offsetMax.x - areaRect.offsetMin.x, 0f, i_Slider.name);

			assertNothingCovers(areaRect, i_Slider.transform);
		}

		/// <summary>
		/// A raycast graphic drawn after the slider and over its TouchArea takes the press there
		/// instead. The difficulty lever's EASY and NORMAL words did, at the area's top corners.
		/// </summary>
		private static void assertNothingCovers(RectTransform i_Area, Transform i_Slider)
		{
			Rect area = worldRect(i_Area);

			for (int i = i_Slider.GetSiblingIndex() + 1; i < i_Slider.parent.childCount; i++)
			{
				foreach (Graphic graphic in i_Slider.parent.GetChild(i).GetComponentsInChildren<Graphic>(true))
				{
					if (graphic.raycastTarget && worldRect(graphic.rectTransform).Overlaps(area))
					{
						Assert.Fail(graphic.name + " is drawn over " + i_Slider.name + "'s TouchArea and takes its presses.");
					}
				}
			}
		}

		private static Rect worldRect(RectTransform i_Rect)
		{
			Vector3[] corners = new Vector3[4];
			i_Rect.GetWorldCorners(corners);

			return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
		}

		/// <summary>Borrows the scene if it is open already, the way PauseOverlaySceneTests does.</summary>
		private void open(string i_Path)
		{
			m_Scene = SceneManager.GetSceneByPath(i_Path);
			m_IsOpenedHere = !m_Scene.isLoaded;

			if (m_IsOpenedHere)
			{
				m_Scene = EditorSceneManager.OpenScene(i_Path, OpenSceneMode.Additive);
			}
		}

		private T find<T>() where T : Component
		{
			foreach (GameObject root in m_Scene.GetRootGameObjects())
			{
				T found = root.GetComponentInChildren<T>(true);

				if (found != null)
				{
					return found;
				}
			}

			Assert.Fail(m_Scene.name + " has no " + typeof(T).Name + ".");
			return null;
		}
	}
}
