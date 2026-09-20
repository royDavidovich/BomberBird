# Arena Grid Design

How a BomberBird arena is described, loaded, queried, and drawn.

This note records decisions and their reasons. Behaviour and scope come from
[GDD.md](GDD.md); this document does not override it.

## Dimensions

A standard arena is **13 x 11 cells**, including the solid border wall. The interior is
11 x 9, holding 20 hard blocks in a lattice and 79 open cells. The boss stage is **15 x 11**,
two cells wider to give the fight room ([GDD.md](GDD.md) section 6).

Chosen for readability. The GDD calls for compact arenas cleared in a few minutes, and with
a burst range of 2 cells and one active pod, a smaller arena keeps a single pod meaningful
without making the stage trivial.

A stage is allowed its own size, so `ArenaLayout.CreateGrid` does not check one against those
numbers. The single rule it enforces is that both dimensions are **odd**, which is what makes
the hard-block lattice symmetric: an even side meets the border with a block against it and
closes the lanes the arena is built from. A ragged row is caught harder still - `ArenaGrid`
throws on it.

One cell is **one Unity world unit**. Tiles are 32 x 32 pixels imported at 32 pixels per
unit, so grid coordinates and world coordinates are the same numbers. This keeps the maths
readable and makes a wrong position obvious on sight.

## Coordinates

- `Vector2Int`, `x` rightward `0..Width-1`, `y` **upward** `0..Height-1`, matching Unity's
  2D axes.
- World position of cell `(x, y)` is `(x - (Width - 1) / 2, y - (Height - 1) / 2)`: the
  centre cell is `(6, 5)` in a standard arena and `(7, 5)` in the boss arena. Dimensions are
  odd, so that centre cell lands exactly on the world origin and the arena stays symmetric
  around the camera at either size.
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
| `c` | Cage, holding the stage's feather |
| `.` | Floor |

A stage that awards a bird needs exactly one `c`. The cage is solid and indestructible and
is opened by the objective rather than by anything the player can aim at it, so it is a cell
state rather than a prop standing on one.

**What is deliberately not in the map text.** Myna and boss starts are `Vector2Int` arrays on
the same `ArenaLayout` asset, not characters: a spawn is a point, not a cell state, and a
character would have to answer what the cell becomes once the myna walks off it. The exit
needs no storage at all - `StageExit.GateCellFor` derives it as `(Width - 1, Height / 2)`,
which is the GDD's "middle of the right-hand wall" written once instead of authored into six
maps. Player start is still the bird's position in the Gameplay scene.

## Structure

Three pieces, each independently understandable:

| Type | Kind | Responsibility |
|---|---|---|
| `ArenaLayout` | ScriptableObject | The authored map text for one stage |
| `ArenaTileSet` | ScriptableObject | The sprites for one habitat |
| `ArenaGrid` | plain C# | Cell state and every question asked about it |
| `ArenaRenderer` | MonoBehaviour | Builds the grid, spawns sprites, follows changes |

`ArenaGrid` needs no GameObject, no scene, and no Play Mode. Burst propagation, movement
blocking, and chain reactions are all decided by calling it, so those rules are tested
directly instead of by watching the screen. That separation is the main reason for
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
bool  TryOpen(Vector2Int i_Cell);          // Cage -> Floor, when the objective says so
Vector2Int CageCell { get; }               // Where this stage's feather waits
Vector3 CellToWorld(Vector2Int i_Cell);    // Cell centre in world space
Vector2Int WorldToCell(Vector3 i_World);   // And back
bool  IsAreaWalkable(Vector2 i_Centre, float i_HalfExtent);   // A body straddling a boundary
event Action<Vector2Int> CellChanged;
```

The predicates answer out-of-bounds safely rather than throwing - a burst travelling off the
map is normal, not exceptional, so `IsBlocking` says yes and `IsWalkable` says no. `GetCell`
is the exception and throws, because a caller asking what a cell *is* outside the arena has a
bug rather than a burst. `TryDestroy` and `TryOpen` return whether anything actually changed,
so a caller never has to re-read the cell to find out.

## Presentation follows state

The grid is authoritative. `ArenaRenderer` subscribes to `CellChanged` and updates the
affected sprite. Nothing reads cell state out of the scene, and removing the renderer would
not change a single rule.

One GameObject per cell: 143 in a standard arena, 165 in the boss arena. Blocks are opaque,
so no floor layer is needed underneath, and destroying a soft block swaps a sprite rather
than destroying an object.

Soft-block variant choice is **deterministic**, hashed from the cell coordinate rather than
drawn from `Random`. A stage therefore looks identical on every run, so a playtest
observation stays reproducible and rebuilding does not make the arena shimmer.

## Validation

A missing layout or tile set logs an error naming the object and disables the component. A
row whose length disagrees with the first row, or an unrecognised character, throws with the
exact row and column, and so does a second `c`, because a stage awards one feather and
therefore holds one cage. An even width or height only warns: the arena still loads, but its
lattice will not meet the border cleanly.

An arena that is silently one cell wrong is far more expensive to find later than a loud
failure at load.

## Camera

Orthographic size **6**, set on the camera in the Gameplay scene rather than computed. The
arena's half-height is 5.5, leaving half a unit of margin. At 16:9 the visible width is about
21 units, against 13 cells in a standard arena and 15 in the boss arena, so there is
deliberate space either side at both sizes. That is where the HUD goes, the way classic
Bomberman uses side panels.

Because the size is fixed and the height is what an orthographic camera pins, a wider display
shows more empty world either side rather than more arena, and a 4:3 display still clears the
boss arena's 15 cells with a unit to spare. `CONVENTIONS.md` section 14.1 calls for genuine
letterbox and pillarbox bars so the frame is identical on every display; that is not built,
and until it is, the margin is what absorbs an off-ratio screen.

## What was built on top

This document's original deliverable was a correct arena on screen and nothing else - no
player, pods, bursts, mynas, exit, or UI. All of those now exist, and every one of them
reaches the arena through the grid surface above rather than by reading the scene:
`BirdMovement` and `MynaWalk` ask `IsAreaWalkable`, `BurstShape` walks `IsBlocking` and
`IsDestructible`, `StageExit` asks `TryOpen`, and `ArenaRenderer` still learns about all of
it from `CellChanged` alone. Removing the renderer would change no rule, which was the point
of the split.
