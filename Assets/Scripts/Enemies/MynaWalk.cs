using System;
using BomberBird.Arena;
using UnityEngine;

namespace BomberBird.Enemies
{
	/// <summary>
	/// Which way a myna goes when it reaches a cell.
	///
	/// It keeps going until the corridor runs out, turns at a junction, and only doubles
	/// back from a dead end. That reads as a bird patrolling rather than one twitching in
	/// place, and it is the whole of the behaviour: a myna never chases and never aims at
	/// the player. The GDD leaves the exact behaviour to prototyping, so this is the first
	/// one to put in front of a playtest, not a decision that has been settled.
	///
	/// Plain C# with no scene and a caller-supplied <see cref="System.Random"/>, so a junction
	/// can be set up and the choice checked instead of watched.
	/// </summary>
	public static class MynaWalk
	{
		private static readonly Vector2Int[] sr_Directions =
		{
			Vector2Int.up,
			Vector2Int.down,
			Vector2Int.left,
			Vector2Int.right,
		};

		/// <summary>
		/// The step to take from <paramref name="i_Cell"/>, or zero when the myna is walled
		/// in on all four sides and has nowhere to go.
		///
		/// <paramref name="i_AlsoBlocked"/> carries what the grid does not know about, such
		/// as a placed pod. Passing null asks the arena's own contents only.
		/// </summary>
		public static Vector2Int ChooseDirection(
			ArenaGrid i_Grid,
			Vector2Int i_Cell,
			Vector2Int i_Current,
			Predicate<Vector2Int> i_AlsoBlocked,
			System.Random i_Random)
		{
			if (i_Grid == null)
			{
				throw new ArgumentNullException("i_Grid");
			}

			if (i_Random == null)
			{
				throw new ArgumentNullException("i_Random");
			}

			Vector2Int back = -i_Current;
			Vector2Int[] options = new Vector2Int[sr_Directions.Length];
			int count = 0;

			for (int i = 0; i < sr_Directions.Length; ++i)
			{
				Vector2Int direction = sr_Directions[i];

				// The way it came is the last resort, so it is left out of the running
				// unless nothing else is open.
				if (direction == back || !canEnter(i_Grid, i_Cell + direction, i_AlsoBlocked))
				{
					continue;
				}

				options[count] = direction;
				++count;
			}

			Vector2Int chosen;

			if (count == 0)
			{
				// A dead end, or a corridor that has just been closed behind it.
				chosen = canEnter(i_Grid, i_Cell + back, i_AlsoBlocked) ? back : Vector2Int.zero;
			}
			else if (count == 1)
			{
				// A corridor. Carrying straight on is the only move that is not a reversal.
				chosen = options[0];
			}
			else
			{
				chosen = options[i_Random.Next(count)];
			}

			return chosen;
		}

		private static bool canEnter(ArenaGrid i_Grid, Vector2Int i_Cell, Predicate<Vector2Int> i_AlsoBlocked)
		{
			return i_Grid.IsWalkable(i_Cell) && (i_AlsoBlocked == null || !i_AlsoBlocked(i_Cell));
		}
	}
}
