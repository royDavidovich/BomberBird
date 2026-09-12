# Asset Credits

Every third-party asset used in BomberBird is recorded here before submission: source, creator, license, and where it is used. Original, student-authored assets do not need an entry.

AI-generated assets are recorded here as well. They are not third-party work, but their provenance is stated rather than left to be inferred.

| Asset | Creator / Source | License | Used for | Notes |
|---|---|---|---|---|
| Arena tile set, 66 PNG across 6 habitat folders | AI-generated with ChatGPT image generation, 2026-09-12. Art direction, review, and approval by Roy Davidovich | Original project asset, no third-party licence | Arena floor, border wall, hard blocks, and soft blocks | `Assets/Art/Tiles/`. 32 x 32, opaque, 32 pixels per unit, point filter, so one tile is exactly one grid cell. Each habitat has 8 soft-block variants chosen at random per cell |
| Eurasian hoopoe sprites, 9 PNG | AI-generated with ChatGPT image generation, 2026-09-12, from the approved concept sheet. Art direction, review, and approval by Roy Davidovich | Original project asset, no third-party licence | The player's starting bird | `Assets/Art/Birds/hoopoe/`. 32 x 32 RGBA, 32 pixels per unit, point filter. Three facings (down, up, side-right) of idle plus a two-frame walk. The left facing is mirrored in engine rather than authored |
| Seed pod sprites, 12 PNG | AI-generated with ChatGPT image generation, 2026-09-12. Art direction, review, and approval by Roy Davidovich | Original project asset, no third-party licence | The placed pod and the burst it becomes | `Assets/Art/Pods/`. 32 x 32 RGBA, 32 pixels per unit, point filter. A three-frame pod pulse, and three fade frames each for the burst centre, arm, and end. The arm is authored horizontal and the end points right; the game rotates them |
| TextMesh Pro Essential Resources, 37 files | Unity Technologies, shipped inside `com.unity.ugui` 2.0.0 | Unity Companion License. Liberation Sans is SIL OFL 1.1, text at `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`. EmojiOne attribution at `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt` | Every piece of TextMesh Pro text: the stage HUD and the pause overlay | `Assets/TextMesh Pro/`, imported 2026-09-13. TextMesh Pro resolves these resources from that exact path, so unlike other third-party material they cannot be moved under `Assets/ThirdParty/` |

## How to add an entry

1. Add a row before importing the asset into the project.
2. Keep the license text or a link to it accessible; do not rely on memory.
3. Place the imported file under a dedicated third-party folder in `Assets/`.
4. Update this file's row if the asset's use changes.
