using System.Collections.Generic;
using UnityEngine;

namespace BomberBird.Pods
{
	/// <summary>
	/// Which cells a burst is still lethal on, and until when.
	///
	/// A burst kills for as long as it is drawn rather than only for the instant it goes
	/// off, because walking through a visible burst unharmed makes a death unreadable.
	/// The bird and the mynas both have to answer that question, so the rule lives here
	/// once instead of once each.
	///
	/// Plain C# with no scene and no clock of its own - the caller passes the time - so a
	/// window can be opened, waited out, and re-opened in a test.
	/// </summary>
	public class BurstDanger
	{
		private readonly Dictionary<Vector2Int, float> r_BurningUntil = new Dictionary<Vector2Int, float>();

		/// <summary>Marks every cell a burst covers as lethal for the next stretch of time.</summary>
		public void Mark(IList<Vector2Int> i_Cells, float i_Now, float i_LethalSeconds)
		{
			if (i_Cells == null)
			{
				return;
			}

			float expiry = i_Now + i_LethalSeconds;

			for (int i = 0; i < i_Cells.Count; ++i)
			{
				Vector2Int cell = i_Cells[i];
				float standing;

				// A second burst over the same cell may only push the danger later, never
				// pull it earlier: a chain reaction must not make a cell safe sooner than
				// the burst already covering it would have.
				if (!r_BurningUntil.TryGetValue(cell, out standing) || expiry > standing)
				{
					r_BurningUntil[cell] = expiry;
				}
			}
		}

		public bool IsBurning(Vector2Int i_Cell, float i_Now)
		{
			float expiry;

			return r_BurningUntil.TryGetValue(i_Cell, out expiry) && i_Now < expiry;
		}

		/// <summary>Forgets every window. For a stage that is starting over.</summary>
		public void Clear()
		{
			r_BurningUntil.Clear();
		}
	}
}
