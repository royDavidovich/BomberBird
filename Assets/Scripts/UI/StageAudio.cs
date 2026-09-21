using System.Collections;
using System.Collections.Generic;
using BomberBird.Enemies;
using BomberBird.Flow;
using BomberBird.Pods;
using UnityEngine;

namespace BomberBird.UI
{
	/// <summary>
	/// The stage's sound effects: one clip per moment the player should hear.
	///
	/// It observes and nothing else, the same way <see cref="StageHud"/> does. Every clip
	/// is played in answer to an event a gameplay component raised, so deleting this
	/// component silences the game without changing a single rule of it. Nothing here ever
	/// decides anything.
	///
	/// The background music is not here. It plays from an AudioSource on the GameFlow
	/// object, which survives the scene reload that a death or a cleared stage triggers, so
	/// the loop runs on instead of restarting every few minutes.
	/// </summary>
	[RequireComponent(typeof(AudioSource))]
	public class StageAudio : MonoBehaviour
	{
		[Header("What it listens to")]
		[SerializeField] private ArenaPods m_Pods;
		[SerializeField] private ArenaMynas m_Mynas;
		[SerializeField] private StageObjective m_Objective;
		[SerializeField] private BirdDeath m_Death;
		[SerializeField] private StageExit m_Exit;

		[Header("Clips")]
		[Tooltip("The bird drops a seed pod.")]
		[SerializeField] private AudioClip m_PodPlace;

		[Tooltip("A pod goes off. The loudest effect in the game.")]
		[SerializeField] private AudioClip m_Burst;

		[Tooltip("A myna is caught in a burst.")]
		[SerializeField] private AudioClip m_MynaDefeated;

		[Tooltip("The last myna falls and the cage gives up the feather. The cage is never "
			+ "struck: clearing the arena is the only thing that opens it.")]
		[SerializeField] private AudioClip m_CageOpen;

		[Tooltip("Seconds to hold the cage's sound back. The last myna dies on the very same "
			+ "frame and its squawk runs about a third of a second, so landing together makes "
			+ "one muddled noise instead of a cause and its effect.")]
		[SerializeField] private float m_CageOpenDelay = 0.4f;

		[Tooltip("The bird picks up the freed feather.")]
		[SerializeField] private AudioClip m_FeatherCollect;

		[Tooltip("The bird is hit and a life is spent.")]
		[SerializeField] private AudioClip m_BirdDeath;

		[Tooltip("The gate in the wall stops being shut. Not the same moment as the stage "
			+ "ending: this is the way out appearing, that is the bird walking into it.")]
		[SerializeField] private AudioClip m_GateOpen;

		[Tooltip("Seconds to hold the gate's sound back, for the same reason the cage's is "
			+ "held: on the three stages that cage no feather the gate opens on the very "
			+ "frame the last myna dies, and on the other three it follows the pickup.")]
		[SerializeField] private float m_GateOpenDelay = 0.4f;

		[Tooltip("The gate is reached and the stage ends.")]
		[SerializeField] private AudioClip m_StageClear;

		private AudioSource m_Source;
		private PodField m_Field;

		private void Awake()
		{
			m_Source = GetComponent<AudioSource>();

			// A looping source here would fight the music, and one that plays on awake would
			// fire whichever clip happened to be left in the slot.
			m_Source.playOnAwake = false;
			m_Source.loop = false;

			if (m_Pods != null)
			{
				m_Field = m_Pods.Field;
			}

			reportUnassigned();
		}

		private void OnEnable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced += podField_PodPlaced;
				m_Field.PodExploded += podField_PodExploded;
			}

			if (m_Mynas != null)
			{
				m_Mynas.MynaDefeated += mynas_MynaDefeated;
			}

			if (m_Objective != null)
			{
				m_Objective.CageOpened += objective_CageOpened;
				m_Objective.FeatherCollected += objective_FeatherCollected;
			}

			if (m_Death != null)
			{
				m_Death.BirdDied += death_BirdDied;
			}

			if (m_Exit != null)
			{
				m_Exit.GateOpened += exit_GateOpened;
				m_Exit.StageCleared += exit_StageCleared;
			}
		}

		private void OnDisable()
		{
			if (m_Field != null)
			{
				m_Field.PodPlaced -= podField_PodPlaced;
				m_Field.PodExploded -= podField_PodExploded;
			}

			if (m_Mynas != null)
			{
				m_Mynas.MynaDefeated -= mynas_MynaDefeated;
			}

			if (m_Objective != null)
			{
				m_Objective.CageOpened -= objective_CageOpened;
				m_Objective.FeatherCollected -= objective_FeatherCollected;
			}

			if (m_Death != null)
			{
				m_Death.BirdDied -= death_BirdDied;
			}

			if (m_Exit != null)
			{
				m_Exit.GateOpened -= exit_GateOpened;
				m_Exit.StageCleared -= exit_StageCleared;
			}
		}

		private void podField_PodPlaced(Vector2Int i_Cell)
		{
			play(m_PodPlace);
		}

		private void podField_PodExploded(Vector2Int i_Origin, IList<Vector2Int> i_Covered)
		{
			play(m_Burst);
		}

		private void mynas_MynaDefeated(Vector2Int i_Cell)
		{
			play(m_MynaDefeated);
		}

		private void objective_CageOpened(Vector2Int i_Cell)
		{
			// Held back rather than played here: see m_CageOpenDelay. Scaled time is right -
			// the arena is running normally when the last myna dies - and a scene load in the
			// meantime takes this component and the wait with it, which is what should happen.
			StartCoroutine(playAfter(m_CageOpenDelay, m_CageOpen));
		}

		private void objective_FeatherCollected(Vector2Int i_Cell)
		{
			play(m_FeatherCollect);
		}

		private void death_BirdDied()
		{
			play(m_BirdDeath);
		}

		private void exit_GateOpened()
		{
			StartCoroutine(playAfter(m_GateOpenDelay, m_GateOpen));
		}

		private void exit_StageCleared()
		{
			playAcrossLoad(m_StageClear);
		}

		/// <summary>
		/// One shot, mixed over whatever is already sounding. A chain reaction is several
		/// bursts in a row, and cutting each one off at the next would make the chain quieter
		/// than a single pod.
		/// </summary>
		private void play(AudioClip i_Clip)
		{
			// PlayOneShot logs an error on a null clip. An unassigned slot should be silence,
			// not console noise every time the player places a pod.
			if (i_Clip != null)
			{
				m_Source.PlayOneShot(i_Clip);
			}
		}

		private IEnumerator playAfter(float i_Seconds, AudioClip i_Clip)
		{
			if (i_Seconds > 0f)
			{
				yield return new WaitForSeconds(i_Seconds);
			}

			play(i_Clip);
		}

		/// <summary>
		/// Plays a clip that has to outlive the scene it started in.
		///
		/// Reaching the gate loads the next stage at the end of the same frame, which
		/// destroys this component and every sound it was playing. The flourish would be cut
		/// off after a few milliseconds. <see cref="UiSound"/> carries it across, and the
		/// menus lean on the same trick for the same reason.
		/// </summary>
		private void playAcrossLoad(AudioClip i_Clip)
		{
			UiSound.Play(i_Clip, m_Source.volume);
		}

		/// <summary>
		/// Says what is unwired and carries on. A missing clip or publisher is a wiring
		/// mistake worth seeing in the console, but it is not a reason to disable the
		/// component and silence the five moments that were wired correctly.
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

			if (m_Death == null)
			{
				missing += " death";
			}

			if (m_Exit == null)
			{
				missing += " exit";
			}

			if (m_PodPlace == null)
			{
				missing += " podPlace";
			}

			if (m_Burst == null)
			{
				missing += " burst";
			}

			if (m_MynaDefeated == null)
			{
				missing += " mynaDefeated";
			}

			if (m_CageOpen == null)
			{
				missing += " cageOpen";
			}

			if (m_FeatherCollect == null)
			{
				missing += " featherCollect";
			}

			if (m_BirdDeath == null)
			{
				missing += " birdDeath";
			}

			if (m_GateOpen == null)
			{
				missing += " gateOpen";
			}

			if (m_StageClear == null)
			{
				missing += " stageClear";
			}

			if (missing.Length > 0)
			{
				Debug.LogWarning(name + ": these stay silent, nothing is assigned:" + missing, this);
			}
		}
	}
}
