using BomberBird.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BomberBird.Tests
{
	/// <summary>
	/// The on-screen touch pad of the Web build on phones, as the gameplay scene authors it. The
	/// bird reads the gamepad these controls pretend to be, so a control path typed wrong in the
	/// Inspector would leave a phone player with a stick that moves nothing, and no error.
	/// </summary>
	public class TouchPadSceneTests
	{
		private const string k_ScenePath = "Assets/Scenes/Gameplay.unity";

		private Scene m_Scene;
		private bool m_IsOpenedHere;
		private Transform m_Pad;

		[SetUp]
		public void SetUp()
		{
			// Borrow the scene if someone has it open, the way PauseOverlaySceneTests does.
			m_Scene = SceneManager.GetSceneByPath(k_ScenePath);
			m_IsOpenedHere = !m_Scene.isLoaded;

			if (m_IsOpenedHere)
			{
				m_Scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
			}

			foreach (GameObject root in m_Scene.GetRootGameObjects())
			{
				if (root.name == "UI")
				{
					m_Pad = root.transform.Find("TouchPad");
				}
			}

			Assert.IsNotNull(m_Pad, "The gameplay scene has no UI/TouchPad.");
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
		public void ThePadShowsOnlyOnAPhone()
		{
			Assert.IsNotNull(m_Pad.GetComponent<MobileOnly>());
		}

		[Test]
		public void TheStickIsTheLeftStick()
		{
			FloatingStick stick = m_Pad.Find("StickZone").GetComponent<FloatingStick>();

			Assert.AreEqual("<Gamepad>/leftStick", stick.controlPath);
		}

		/// <summary>
		/// The stick's zone covers the whole screen, so it has to be drawn first: the buttons
		/// after it keep their own taps instead of starting a drag.
		/// </summary>
		[Test]
		public void TheButtonsAreDrawnOverTheStickZone()
		{
			Transform zone = m_Pad.Find("StickZone");

			Assert.AreEqual(0, zone.GetSiblingIndex());
			Assert.Greater(m_Pad.Find("PodButton").GetSiblingIndex(), zone.GetSiblingIndex());
			Assert.Greater(m_Pad.Find("PauseButton").GetSiblingIndex(), zone.GetSiblingIndex());
		}

		[Test]
		public void ThePodButtonIsTheSouthButton()
		{
			OnScreenButton pod = m_Pad.Find("PodButton").GetComponent<OnScreenButton>();

			Assert.AreEqual("<Gamepad>/buttonSouth", pod.controlPath);
		}

		[Test]
		public void ThePauseButtonPauses()
		{
			Button pause = m_Pad.Find("PauseButton").GetComponent<Button>();

			Assert.AreEqual(1, pause.onClick.GetPersistentEventCount());
			Assert.IsInstanceOf<PauseController>(pause.onClick.GetPersistentTarget(0));
			Assert.AreEqual("Pause", pause.onClick.GetPersistentMethodName(0));
		}
	}
}
