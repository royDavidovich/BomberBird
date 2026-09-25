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
	/// The pause overlay as it is authored in the gameplay scene: that the arrow keys walk its
	/// three buttons and two volume sliders and stop at the ends, and that its buttons, and the
	/// rules panel it opens, confirm with a sound.
	///
	/// Read from the real scene rather than a rebuilt copy, because what can break here is
	/// the wiring itself - a link or a clip dropped in the Inspector - and a copy would carry
	/// the test's idea of the wiring instead of the scene's.
	/// </summary>
	public class PauseOverlaySceneTests
	{
		private const string k_ScenePath = "Assets/Scenes/Gameplay.unity";

		private Scene m_Scene;
		private bool m_IsOpenedHere;
		private PauseController m_Pause;

		[SetUp]
		public void SetUp()
		{
			// Someone may be editing the scene already. Borrow it rather than open it twice,
			// and leave it open afterwards, or closing it would throw away their unsaved work.
			m_Scene = SceneManager.GetSceneByPath(k_ScenePath);
			m_IsOpenedHere = !m_Scene.isLoaded;

			if (m_IsOpenedHere)
			{
				m_Scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
			}

			m_Pause = findPauseController(m_Scene);
			Assert.IsNotNull(m_Pause, "The gameplay scene has no PauseController.");
		}

		[TearDown]
		public void TearDown()
		{
			if (m_IsOpenedHere && m_Scene.isLoaded)
			{
				EditorSceneManager.CloseScene(m_Scene, true);
			}
		}

		/// <summary>
		/// The arrows walk the three buttons and the two sliders in order and stop at both ends,
		/// the way the end of a list should (af04158). Automatic navigation used to pick the
		/// nearest selectable anywhere on screen and wander onto the lifebuoy, so the links are
		/// Explicit.
		///
		/// Read straight off the controls rather than through
		/// <see cref="Selectable.FindSelectableOnUp"/>: the overlay is inactive in the saved
		/// scene, and if the mode ever slipped back to Automatic that call would find nothing
		/// here and pass the ends for the wrong reason.
		/// </summary>
		[Test]
		public void TheArrowKeysWalkThePauseMenuAndStopAtItsEnds()
		{
			Button resume = findOverlay<Button>("Resume");
			Button restart = findOverlay<Button>("Restart");
			Button mainMenu = findOverlay<Button>("MainMenuButton");
			Slider music = findOverlay<Slider>("MusicSlider");
			Slider effects = findOverlay<Slider>("EffectsSlider");

			Selectable[] chain = { resume, restart, mainMenu, music, effects };

			for (int i = 0; i < chain.Length; ++i)
			{
				Assert.AreEqual(Navigation.Mode.Explicit, chain[i].navigation.mode, chain[i].name);
				Assert.AreSame(i > 0 ? chain[i - 1] : null, chain[i].navigation.selectOnUp, "Up from " + chain[i].name);
				Assert.AreSame(
					i < chain.Length - 1 ? chain[i + 1] : null, chain[i].navigation.selectOnDown, "Down from " + chain[i].name);
			}
		}

		/// <summary>
		/// A horizontal slider only takes Left and Right as a change of value while it has
		/// nothing to move to on that side. A link there would turn the arrows that set the
		/// volume into arrows that leave it.
		/// </summary>
		[Test]
		public void LeftAndRightAreLeftToTheVolumeSliders()
		{
			foreach (string name in new[] { "MusicSlider", "EffectsSlider" })
			{
				Slider slider = findOverlay<Slider>(name);

				Assert.AreEqual(Slider.Direction.LeftToRight, slider.direction, name);
				Assert.IsNull(slider.navigation.selectOnLeft, name);
				Assert.IsNull(slider.navigation.selectOnRight, name);
			}
		}

		[Test]
		public void TheVolumeSlidersAreWiredToThePauseController()
		{
			Assert.AreSame(findOverlay<Slider>("MusicSlider"), readReference(m_Pause, "m_MusicSlider"));
			Assert.AreSame(findOverlay<Slider>("EffectsSlider"), readReference(m_Pause, "m_EffectsSlider"));
			Assert.IsNotNull(readReference(m_Pause, "m_EffectsPreview"), "The effects slider would change silently.");
		}

		[Test]
		public void ThePauseButtonsHaveAConfirmSound()
		{
			Assert.IsNotNull(readReference(m_Pause, "m_Confirm"));
		}

		/// <summary>
		/// The rules panel opened from the overlay's Help button plays its clip on closing, from
		/// a field of its own, so wiring the overlay's clip does not wire this one.
		/// </summary>
		[Test]
		public void TheRulesPanelHasAConfirmSound()
		{
			InstructionsPanel instructions = readReference(m_Pause, "m_Instructions") as InstructionsPanel;

			Assert.IsNotNull(instructions, "The pause overlay has no rules panel wired.");
			Assert.IsNotNull(readReference(instructions, "m_Confirm"));
		}

		private static PauseController findPauseController(Scene i_Scene)
		{
			PauseController found = null;
			GameObject[] roots = i_Scene.GetRootGameObjects();

			for (int i = 0; i < roots.Length && found == null; ++i)
			{
				found = roots[i].GetComponentInChildren<PauseController>(true);
			}

			return found;
		}

		/// <summary>
		/// Looked up under the overlay the controller shows, so a control of the same name
		/// elsewhere in the scene cannot stand in for it.
		/// </summary>
		private T findOverlay<T>(string i_Name) where T : Selectable
		{
			GameObject overlay = readReference(m_Pause, "m_Overlay") as GameObject;
			Assert.IsNotNull(overlay, "The PauseController has no overlay wired.");

			Transform child = overlay.transform.Find(i_Name);
			Assert.IsNotNull(child, "The pause overlay has no " + i_Name + ".");

			T control = child.GetComponent<T>();
			Assert.IsNotNull(control, i_Name + " on the pause overlay is not a " + typeof(T).Name + ".");

			return control;
		}

		private static Object readReference(Object i_Target, string i_Field)
		{
			SerializedProperty property = new SerializedObject(i_Target).FindProperty(i_Field);
			Assert.IsNotNull(property, i_Field + " was renamed; the scene wiring is stale too");

			return property.objectReferenceValue;
		}
	}
}
