# Arena Grid Design

How a BomberBird arena is described, loaded, queried, and drawn.

This note records decisions and their reasons. Behaviour and scope come from
[GDD.md](GDD.md); this document does not override it.

## Dimensions

An arena is **13 x 11 cells**, including the solid border wall. The interior is 11 x 9,
holding 20 hard blocks in a lattice and 79 open cells.

Chosen for readability. The GDD calls for compact arenas cleared in a few minutes, and with
a burst range of 2 cells and one active pod, a smaller arena keeps a single pod meaningful
without making the stage trivial. Odd interior dimensions make the hard-block lattice
symmetric.

One cell is **one Unity world unit**. Tiles are 32 x 32 pixels imported at 32 pixels per
unit, so grid coordinates and world coordinates are the same numbers. This keeps the maths
readable and makes a wrong position obvious on sight.

## Coordinates

- `Vector2Int`, `x` rightward `0..12`, `y` **upward** `0..10`, matching Unity's 2D axes.
- World position of cell `(x, y)` is `(x - 6, y - 5)`. Dimensions are odd, so the centre
  cell `(6, 5)` sits exactly on the world origin and the arena is symmetric around the
  camera.
- Row `0` of an authored layout is the **top** row, because that is how a person reads a
  map. The parser flips it: `y = Height - 1 - rowIndex`.

That flip is the most likely place for an off-by-one error in the whole system, which is
why it has a dedicated test.

## Layout data

A stage is an `ArenaLayout` ScriptableObject holding one multi-line string. The map is
authored as text so the shape is visible while editing, and so a stage is a data change
rather than a scene edit.

| Char | Cell |
|---|---|
| `#` | Border |
| `H` | HardBlock |
| `s` | SoftBlock |
| `.` | Floor |

Player start, enemy spawns, and the exit are **not** in the format yet. They are not part
of building a grid, and inventing storage for them before the player and mynas exist would
be guessing at interfaces. Adding characters later is a text edit.

## Structure

Three pieces, each independently understandable:

| Type | Kind | Responsibility |
|---|---|---|
| `ArenaLayout` | ScriptableObject | The authored map text for one stage |
| `ArenaTileSet` | ScriptableObject | The sprites for one habitat |
| `ArenaGrid` | plain C# | Cell state and every question asked about it |
| `ArenaRenderer` | MonoBehaviour | Builds the grid, spawns sprites, follows changes |

`ArenaGrid` needs no GameObject, no scene, and no Play Mode. Burst propagation, movement
blocking, and chain reactions will all be decided by calling it, so those rules can be
tested directly instead of by watching the screen. That separation is the main reason for
this structure.

`ArenaGrid` uses `Vector2Int`, which lives in `UnityEngine`. It is a plain struct, not
engine machinery, and it does not pull a scene into the tests.

### Grid surface

```csharp
eCell GetCell(Vector2Int i_Cell);
bool  IsInside(Vector2Int i_Cell);
bool  IsBlocking(Vector2Int i_Cell);       // Border, HardBlock, SoftBlock stop a burst
bool  IsDestructible(Vector2Int i_Cell);   // SoftBlock only
bool  IsWalkable(Vector2Int i_Cell);       // Floor only
bool  TryDestroy(Vector2Int i_Cell);       // SoftBlock -> Floor
event Action<Vector2Int> CellChanged;
```

Out-of-bounds queries answer safely rather than throwing: a burst travelling off the map is
normal, not exceptional. `TryDestroy` returns whether anything actually changed, so a caller
never has to re-read the cell to find out.

## Presentation follows state

The grid is authoritative. `ArenaRenderer` subscribes to `CellChanged` and updates the
affected sprite. Nothing reads cell state out of the scene, and removing the renderer would
not change a single rule.

One GameObject per cell, 143 in total. Blocks are opaque, so no floor layer is needed
underneath, and destroying a soft block swaps a sprite rather than destroying an object.

Soft-block variant choice is **deterministic**, hashed from the cell coordinate rather than
drawn from `Random`. A stage therefore looks identical on every run, so a playtest
observation stays reproducible and rebuilding does not make the arena shimmer.

## Validation

A missing layout or tile set logs an error naming the object and disables the component. A
row count or row length that disagrees with the declared dimensions, or an unrecognised
character, throws with the exact row and column.

An arena that is silently one cell wrong is far more expensive to find later than a loud
failure at load.

## Camera

Orthographic size **6**. The arena's half-height is 5.5, leaving half a unit of margin. At
16:9 the visible width is about 21 units against a 13-wide arena, so there is deliberate
space either side. That is where the HUD goes, the way classic Bomberman uses side panels.

This matches the fixed-aspect decision in the local conventions: the camera frames the arena
identically for every player, and off-ratio screens get bars rather than a different view of
the world.

## Deliberately not here

No player, movement, seed pods, bursts, mynas, exit, or UI. The deliverable is a correct
arena on screen. Everything else builds on the grid surface above.
