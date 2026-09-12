using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// The sprites a seed pod and its burst are drawn with. Data only: it hands out frames
	/// and never decides when a frame changes, so swapping this asset reskins a pod without
	/// touching a rule or a timing.
	///
	/// The three burst pieces are authored once and rotated in engine: the arm is drawn
	/// horizontally and the end is drawn pointing right. The art carries no directional
	/// lighting, so a rotated copy is indistinguishable from an authored one.
	/// </summary>
	[CreateAssetMenu(fileName = "podset", menuName = "BomberBird/Pod Sprite Set")]
	public class PodSpriteSet : ScriptableObject
	{
		[Header("The pod on the ground")]
		[Tooltip("Pulse frames, calmest first. Looping them faster is how a running fuse reads as urgent.")]
		[SerializeField] private Sprite[] m_PodPulse;

		[Header("Burst pieces, strongest frame first")]
		[Tooltip("The cell the pod was on.")]
		[SerializeField] private Sprite[] m_BurstCentre;

		[Tooltip("A middle segment of an arm. Authored horizontal; rotated for the other three directions.")]
		[SerializeField] private Sprite[] m_BurstArm;

		[Tooltip("The outermost segment of an arm. Authored pointing right; rotated for the other three directions.")]
		[SerializeField] private Sprite[] m_BurstEnd;

		/// <summary>
		/// How many frames a burst fades over. Zero when the set is incomplete, so a caller
		/// that drives the fade from this value simply draws nothing rather than misbehaving.
		/// </summary>
		public int BurstFrameCount
		{
			get { return m_BurstCentre == null ? 0 : m_BurstCentre.Length; }
		}

		/// <summary>A pulse frame, wrapping so any step index is valid.</summary>
		public Sprite GetPodFrame(int i_Step)
		{
			return wrapFrame(m_PodPulse, i_Step);
		}

		/// <summary>One piece of a burst, on the given fade frame.</summary>
		public Sprite GetBurst(eBurstPiece i_Piece, int i_Frame)
		{
			Sprite[] frames;

			switch (i_Piece)
			{
				case eBurstPiece.Arm:
					frames = m_BurstArm;
					break;

				case eBurstPiece.End:
					frames = m_BurstEnd;
					break;

				default:
					frames = m_BurstCentre;
					break;
			}

			return clampFrame(frames, i_Frame);
		}

		/// <summary>Reports every problem with the set at once, or null when it is complete.</summary>
		public string DescribeMissingSprites()
		{
			string missing = string.Empty;

			if (m_PodPulse == null || m_PodPulse.Length == 0)
			{
				missing += " podPulse";
			}

			if (m_BurstCentre == null || m_BurstCentre.Length == 0)
			{
				missing += " burstCentre";
			}

			if (m_BurstArm == null || m_BurstArm.Length == 0)
			{
				missing += " burstArm";
			}

			if (m_BurstEnd == null || m_BurstEnd.Length == 0)
			{
				missing += " burstEnd";
			}

			// The three pieces are drawn as one burst on the same frame index, so a short
			// array would quietly freeze that piece while the others kept fading.
			if (missing.Length == 0 && !haveMatchingBurstFrameCounts())
			{
				missing += " burstFrameCounts (centre, arm, and end must have the same number of frames)";
			}

			return missing.Length == 0 ? null : missing.Trim();
		}

		private bool haveMatchingBurstFrameCounts()
		{
			return m_BurstArm.Length == m_BurstCentre.Length && m_BurstEnd.Length == m_BurstCentre.Length;
		}

		/// <summary>Loops: the pod pulses for as long as its fuse runs.</summary>
		private static Sprite wrapFrame(Sprite[] i_Frames, int i_Step)
		{
			if (i_Frames == null || i_Frames.Length == 0)
			{
				return null;
			}

			int index = ((i_Step % i_Frames.Length) + i_Frames.Length) % i_Frames.Length;

			return i_Frames[index];
		}

		/// <summary>
		/// Plays once: a burst fades and is gone. An index past the last frame holds the
		/// faintest one instead of snapping back to full force.
		/// </summary>
		private static Sprite clampFrame(Sprite[] i_Frames, int i_Frame)
		{
			if (i_Frames == null || i_Frames.Length == 0)
			{
				return null;
			}

			int index = Mathf.Clamp(i_Frame, 0, i_Frames.Length - 1);

			return i_Frames[index];
		}
	}
}
