using BomberBird.Arena;
using BomberBird.Player;
using NUnit.Framework;
using UnityEngine;

namespace BomberBird.Tests
{
	/// <summary>
	/// Corner assist should turn a near-miss into a turn, and should never drag the bird
	/// sideways against a wall it still cannot pass.
	///
	/// Every case places the bird flush against the obstruction above it, because that is
	/// where it actually comes to rest. Testing from a cell's centre proves nothing: the
	/// bird is still a fraction of a cell short of touching anything.
	/// </summary>
	public class CornerAssistTests
	{
		private const float k_Step = 0.1f;
		private const float k_HalfExtent = 0.35f;
		private const float k_MaxAssist = 0.4f;

		// Row 0 is the top row. Interior rows alternate open and lattice.
		// In a lattice row the hard blocks sit on even x, so odd columns are the gaps.
		private const string k_Rows =
			"#############\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#.H.H.H.H.H.#\n" +
			"#...........#\n" +
			"#############";

		private static ArenaGrid makeGrid()
		{
			return new ArenaGrid(k_Rows);
		}

		/// <summary>
		/// A position inside <paramref name="i_Cell"/> whose top edge just touches the
		/// boundary with the cell above, offset sideways by <paramref name="i_XOffset"/>.
		/// </summary>
		private static Vector2 flushUnderCeiling(ArenaGrid i_Grid, Vector2Int i_Cell, float i_XOffset)
		{
			Vector3 centre = i_Grid.CellToWorld(i_Cell);

			return new Vector2(centre.x + i_XOffset, centre.y + 0.5f - k_HalfExtent);
		}

		[Test]
		public void DoesNotSlide_WhenPressingIntoASolidWall()
		{
			// The reported bug. Pressed against the top border, slightly off the centre
			// line, holding up. Sliding sideways cannot help: the border spans the row.
			ArenaGrid grid = makeGrid();
			Assert.AreEqual(eCell.Border, grid.GetCell(new Vector2Int(3, 10)));

			Vector2 from = flushUnderCeiling(grid, new Vector2Int(3, 9), 0.2f);

			Vector2 next;
			bool assisted = BirdMovement.TryCornerAssist(
				grid, from, Vector2Int.up, k_Step, k_HalfExtent, k_MaxAssist, out next);

			Assert.IsFalse(assisted, "must not slide along a wall that spans the whole row");
			Assert.AreEqual(from, next, "the bird must stay exactly where it is");
		}

		[Test]
		public void Slides_WhenAlignmentWouldOpenTheGap()
		{
			// Pressed against a lattice row, straddling a hard block and the gap beside it.
			// Aligning to the gap's centre line genuinely opens the way up.
			ArenaGrid grid = makeGrid();
			Assert.AreEqual(eCell.HardBlock, grid.GetCell(new Vector2Int(2, 6)), "block above column 2");
			Assert.AreEqual(eCell.Floor, grid.GetCell(new Vector2Int(3, 6)), "gap above column 3");

			Vector2 from = flushUnderCeiling(grid, new Vector2Int(3, 5), -0.2f);

			// Confirm the premise: straight up really is blocked from here.
			Assert.IsFalse(
				grid.IsAreaWalkable(from + Vector2.up * k_Step, k_HalfExtent),
				"the bird should be overlapping the hard block's column");

			Vector2 next;
			bool assisted = BirdMovement.TryCornerAssist(
				grid, from, Vector2Int.up, k_Step, k_HalfExtent, k_MaxAssist, out next);

			Assert.IsTrue(assisted, "aligning would clear the gap, so it should assist");
			Assert.Greater(next.x, from.x, "should slide toward the gap's centre line");
			Assert.AreEqual(from.y, next.y, "must not move along the travel axis");
		}

		[Test]
		public void DoesNothing_WhenAssistIsDisabled()
		{
			ArenaGrid grid = makeGrid();
			Vector2 from = flushUnderCeiling(grid, new Vector2Int(3, 5), -0.2f);

			Vector2 next;
			bool assisted = BirdMovement.TryCornerAssist(
				grid, from, Vector2Int.up, k_Step, k_HalfExtent, 0f, out next);

			Assert.IsFalse(assisted, "zero tolerance must disable the behaviour entirely");
			Assert.AreEqual(from, next);
		}

		[Test]
		public void DoesNothing_WhenTooFarOffTheCentreLine()
		{
			ArenaGrid grid = makeGrid();
			Vector2 from = flushUnderCeiling(grid, new Vector2Int(3, 5), -0.45f);

			Vector2 next;
			bool assisted = BirdMovement.TryCornerAssist(
				grid, from, Vector2Int.up, k_Step, k_HalfExtent, k_MaxAssist, out next);

			Assert.IsFalse(assisted, "beyond the tolerance the bird is not 'nearly aligned'");
			Assert.AreEqual(from, next);
		}

		[Test]
		public void NeverSlidesPastTheCentreLine()
		{
			ArenaGrid grid = makeGrid();
			Vector3 centre = grid.CellToWorld(new Vector2Int(3, 5));
			Vector2 from = flushUnderCeiling(grid, new Vector2Int(3, 5), -0.02f);

			Vector2 next;
			bool assisted = BirdMovement.TryCornerAssist(
				grid, from, Vector2Int.up, k_Step, k_HalfExtent, k_MaxAssist, out next);

			Assert.IsTrue(assisted);
			Assert.AreEqual(centre.x, next.x, 0.0001f, "should land on the centre line, not overshoot it");
		}
	}
}
