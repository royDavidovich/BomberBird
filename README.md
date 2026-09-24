# BomberBird

![The BomberBird title screen: a stone gate at dusk with the four playable birds perched on the left and a flock of common mynas on the right](Docs/Images/main-menu.png)

BomberBird is a single-player reinterpretation of a classic grid action game, built for the
final project of an introductory Unity game development course. It takes Israeli birds and
habitats as its theme: the player controls a local bird defending its habitat from invasive
common mynas, moving through compact grid arenas and placing timed seed pods instead of
bombs to clear obstacles, defeat enemies, and reach the exit.

**Play it in the browser: [roydavidovich.itch.io/bomberbird](https://roydavidovich.itch.io/bomberbird)**

## How to play

![The in-game How to Play card, showing three panels: walk with WASD or the arrow keys, place a pod with Space and step away, and the pod bursting in four directions two seconds later](Docs/Images/how-to-play.png)

Clear every common myna from the arena. A burst does not care who placed it, so the pod that
opens a path will take the bird that placed it just as readily. Some habitats cage a feather:
clear the arena, collect the feather, and the exit gate opens.

![Stage one of BomberBird in play: on the left, the olive-tree HUD with the garden seen through its canopy, the stage named on its sign and six hearts in its trunk; on the right, a walled green arena of hard blocks and soft bushes, the hoopoe in the top-left corner, three mynas spread across the grid, and the exit gate set in the right-hand wall](Docs/Images/arena-stage-1.png)

## The birds

Four native birds are playable. The hoopoe starts the campaign; the other three are earned by
collecting the feather their habitat awards.

| Bird | Speed | Burst | Pods | Unlocked by |
|---|---|---|---|---|
| [Eurasian hoopoe](Docs/ArtReferences/Birds/hoopoe-approved-concept.png) | 4 | 2 | 1 | the starting bird |
| [White-throated kingfisher](Docs/ArtReferences/Birds/white-throated-kingfisher-approved-concept.png) | 5.5 | 1 | 2 | the Stream Bank feather |
| [Great white pelican](Docs/ArtReferences/Birds/great-white-pelican-approved-concept.png) | 3 | 3 | 1 | the Lagoon Shore feather |
| [Chukar partridge](Docs/ArtReferences/Birds/chukar-partridge-approved-concept.png) | 4.5 | 2 | 2 | the Rocky Upland feather |

The enemy is the [common myna](Docs/ArtReferences/Birds/common-myna-approved-concept.png), an
invasive species that takes the nesting cavities the native birds depend on. A larger one holds
the final stage.

![The bird selection screen: four field-guide cards over a dusk orchard, showing the hoopoe, kingfisher and pelican with their speed, burst and pod numbers, and a fourth card still locked](Docs/Images/bird-select.png)

## The habitats

Six handmade stages, each its own habitat, each with its own tile set and its own painted
intro: First Steps Garden, Orchard Grove, Stream Bank, Lagoon Shore, Rocky Upland, and the
Overgrown Courtyard where the boss waits.

| | |
|---|---|
| ![Stage one intro card, First Steps Garden: a walled garden at dusk with a pomegranate tree and a hoopoe on the path](Docs/Images/habitat-1-first-steps-garden.png) | ![Stage four intro card, Lagoon Shore: a pelican wading in shallow brackish water under a low sun](Docs/Images/habitat-4-lagoon-shore.png) |

![Stage six intro card, Overgrown Courtyard: a dead tree whose every cavity holds a common myna, over a cracked stone courtyard at nightfall](Docs/Images/habitat-6-overgrown-courtyard.png)

## Status

The campaign is playable end to end: six handmade stages, each in its own habitat, from the
garden intro to the common myna boss. The player starts as the hoopoe and earns the other
three birds by collecting the feather each habitat awards, and the run carries a menu, bird
selection, results, game over, and the two closing screens around them.

Remaining before submission: fresh macOS and Windows builds, each smoke-tested on its own
platform.

The full design is in [Docs/GDD.md](Docs/GDD.md); section 8 tracks what is built against what
is scoped.

## Controls

| Action | Keyboard |
|---|---|
| Move | WASD or arrow keys |
| Place seed pod | Space |
| Confirm or retry | Enter or Space |
| Pause or back | Escape |

## Documentation

- [Docs/GDD.md](Docs/GDD.md): Game Design Document, and the source of truth for behaviour and scope
- [Docs/ArenaGrid.md](Docs/ArenaGrid.md): how an arena is described, loaded, queried, and drawn
- [Docs/DEVELOPMENT_GUIDELINES.md](Docs/DEVELOPMENT_GUIDELINES.md): development and Git workflow rules
- [Docs/ASSET_CREDITS.md](Docs/ASSET_CREDITS.md): third-party asset sources and licenses
- [Docs/ArtReferences/Birds/](Docs/ArtReferences/Birds/): the approved concept sheets behind the bird sprites

Every asset in the game, original or third-party, is recorded with its source and licence in
[Docs/ASSET_CREDITS.md](Docs/ASSET_CREDITS.md).
