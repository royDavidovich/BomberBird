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
	/// The pause overlay as bird selection carries it: the same prefab as the arena's, with
	/// Restart hidden, because nothing has started yet that could be restarted.
	///
	/// Read from the real scene, like <see cref="PauseOverlaySceneTests"/>, because what breaks
	/// here is wiring made in the Inspector: a button still pointing at nothing, or an arrow
	/// link still pointing at the hidden Restart, which would strand the focus on Resume.
	/// </summary>
	public class BirdSelectPauseSceneTests
	{
		private const string k_ScenePath = "Assets/Scenes/BirdSelect.unity";

		private Scene m_Scene;
		private bool m_IsOpenedHere;
		private PauseController m_Pause;

		[SetUp]
		public void SetUp()
		{
			// Borrow the scene if someone has it open, so closing it cannot lose their work.
			m_Scene = SceneManager.GetSceneByPath(k_ScenePath);
			m_IsOpenedHere = !m_Scene.isLoaded;

			if (m_IsOpenedHere)
			{
				m_Scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
			}

			m_Pause = findInScene<PauseController>();
			Assert.IsNotNull(m_Pause, "Bird selection has no PauseController.");
		}

		[TearDown]
		public void TearDown()
		{
			if (m_IsOpenedHere && m_Scene.isLoaded)
			{
				EditorSceneManager.CloseScene(m_Scene, true);
			}
		}

		[Test]
		public void RestartIsHiddenOnBirdSelection()
		{
			Assert.IsFalse(findOverlay<Button>("Restart").gameObject.activeSelf);
		}

		/// <summary>
		/// Resume and Main Menu link to each other directly. UGUI will not move the focus onto a
		/// hidden button, so a link left pointing at Restart would be a dead arrow.
		/// </summary>
		[Test]
		public void TheArrowKeysSkipTheHiddenRestartAndStopAtTheEnds()
		{
			Selectable[] chain =
			{
				findOverlay<Button>("Resume"),
				findOverlay<Button>("MainMenuButton"),
				findOverlay<Slider>("MusicSlider"),
				findOverlay<Slider>("EffectsSlider"),
			};

			for (int i = 0; i < chain.Length; ++i)
			{
				Assert.AreEqual(Navigation.Mode.Explicit, chain[i].navigation.mode, chain[i].name);
				Assert.AreSame(i > 0 ? chain[i - 1] : null, chain[i].navigation.selectOnUp, "Up from " + chain[i].name);
				Assert.AreSame(
					i < chain.Length - 1 ? chain[i + 1] : null, chain[i].navigation.selectOnDown, "Down from " + chain[i].name);
			}
		}

		/// <summary>
		/// The prefab's buttons point at the arena's PauseController there, as instance
		/// overrides. Here each has to be pointed at this scene's own.
		/// </summary>
		[TestCase("Resume", "Resume")]
		[TestCase("MainMenuButton", "GoToMainMenu")]
		[TestCase("PausePanel/HelpIcon", "ShowInstructions")]
		public void TheOverlayButtonsCallThisScenesPauseController(string i_Button, string i_Method)
		{
			assertCallsPause(findOverlay<Button>(i_Button), i_Method);
		}

		[Test]
		public void ThePhonePauseButtonCallsThisScenesPauseController()
		{
			Button pauseButton = null;

			foreach (MobileOnly mobileOnly in findAllInScene<MobileOnly>())
			{
				Button button = mobileOnly.GetComponent<Button>();

				if (button != null)
				{
					pauseButton = button;
				}
			}

			Assert.IsNotNull(pauseButton, "Bird selection has no phone-only pause button.");
			assertCallsPause(pauseButton, "Pause");
		}

		[Test]
		public void TheVolumeSlidersAndSoundsAreWired()
		{
			Assert.AreSame(findOverlay<Slider>("MusicSlider"), readReference(m_Pause, "m_MusicSlider"));
			Assert.AreSame(findOverlay<Slider>("EffectsSlider"), readReference(m_Pause, "m_EffectsSlider"));
			Assert.IsNotNull(readReference(m_Pause, "m_EffectsPreview"));
			Assert.IsNotNull(readReference(m_Pause, "m_Confirm"));
		}

		[Test]
		public void TheLifebuoyHasARulesPanelToOpen()
		{
			InstructionsPanel instructions = readReference(m_Pause, "m_Instructions") as InstructionsPanel;

			Assert.IsNotNull(instructions, "The pause overlay has no rules panel wired.");
			Assert.IsNotNull(readReference(instructions, "m_Panel"));
			Assert.IsNotNull(readReference(instructions, "m_Confirm"));
		}

		/// <summary>
		/// Without this link the card row's first-key helper would remember the overlay's
		/// buttons as the last thing touched, and bring back a hidden Resume after the menu
		/// closes.
		/// </summary>
		[Test]
		public void TheCardRowStandsDownWhileTheMenuIsUp()
		{
			FirstKeySelection cardRow = null;

			foreach (FirstKeySelection selection in findAllInScene<FirstKeySelection>())
			{
				if (selection.GetComponent<BirdSelectScreen>() != null)
				{
					cardRow = selection;
				}
			}

			Assert.IsNotNull(cardRow, "Bird selection has no FirstKeySelection beside its screen.");
			Assert.AreSame(m_Pause, readReference(cardRow, "m_Pause"));
		}

		private void assertCallsPause(Button i_Button, string i_Method)
		{
			Assert.AreEqual(1, i_Button.onClick.GetPersistentEventCount(), i_Button.name);
			Assert.AreSame(m_Pause, i_Button.onClick.GetPersistentTarget(0), i_Button.name);
			Assert.AreEqual(i_Method, i_Button.onClick.GetPersistentMethodName(0), i_Button.name);
		}

		private T findInScene<T>() where T : Component
		{
			T[] all = findAllInScene<T>();

			return all.Length > 0 ? all[0] : null;
		}

		private T[] findAllInScene<T>() where T : Component
		{
			System.Collections.Generic.List<T> found = new System.Collections.Generic.List<T>();

			foreach (GameObject root in m_Scene.GetRootGameObjects())
			{
				found.AddRange(root.GetComponentsInChildren<T>(true));
			}

			return found.ToArray();
		}

		private T findOverlay<T>(string i_Path) where T : Component
		{
			GameObject overlay = readReference(m_Pause, "m_Overlay") as GameObject;
			Assert.IsNotNull(overlay, "The PauseController has no overlay wired.");

			Transform child = overlay.transform.Find(i_Path);
			Assert.IsNotNull(child, "The pause overlay has no " + i_Path + ".");

			T control = child.GetComponent<T>();
			Assert.IsNotNull(control, i_Path + " on the pause overlay is not a " + typeof(T).Name + ".");

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
