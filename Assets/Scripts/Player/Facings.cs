using UnityEngine;

namespace BomberBird.Player
{
	/// <summary>
	/// Turns a step on the grid into the facing that should be drawn for it.
	///
	/// Shared because both walkers need the same answer: the bird from the direction the
	/// player is holding, a myna from the direction it chose for itself. A second copy of
	/// this switch is a second place for the sprites to disagree with the movement.
	/// </summary>
	public static class Facings
	{
		/// <summary>
		/// The facing for a step. A step along both axes resolves to the horizontal one,
		/// which is what the side sprites are for, and a zero step keeps the caller's
		/// current facing by returning <paramref name="i_Current"/>.
		/// </summary>
		public static eFacing FromStep(Vector2Int i_Step, eFacing i_Current)
		{
			eFacing facing;

			if (i_Step.x > 0)
			{
				facing = eFacing.Right;
			}
			else if (i_Step.x < 0)
			{
				facing = eFacing.Left;
			}
			else if (i_Step.y > 0)
			{
				facing = eFacing.Up;
			}
			else if (i_Step.y < 0)
			{
				facing = eFacing.Down;
			}
			else
			{
				facing = i_Current;
			}

			return facing;
		}
	}
}
