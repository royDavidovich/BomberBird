# BomberBird

BomberBird is a single-player reinterpretation of a classic grid action game, built for the final project of an introductory Unity game development course.

The game uses Israeli birds and habitats as its original theme: the player controls a local bird defending its habitat from invasive common mynas, moving through compact grid arenas and placing timed seed pods instead of bombs to clear obstacles, defeat enemies, and reach the exit.

## Status

The campaign is playable end to end: six handmade stages, each in its own habitat, from the garden intro to the common myna boss. The player starts as the hoopoe and earns the other three birds by collecting the feather each habitat awards, and the run carries a menu, bird selection, results, game over, and the two closing screens around them.

Remaining before submission: fresh macOS and Windows builds, each smoke-tested on its own platform.

The full design is in [Docs/GDD.md](Docs/GDD.md); section 8 tracks what is built against what is scoped.

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
