# MAP_CONCEPT — Independent island map blueprint

**Status: BW-MAP1 implemented in source, awaiting Godot build/F6 visual validation.** This is a **2D concept view** for designing and iterating on the future finite island, inspired loosely by illustrated fantasy maps. It does **not** generate terrain chunks, use Terrain3D, stream objects or change `SCENE/Scene_World.tscn`.

## Open and navigate

1. Pull the latest Ben Final `main`, allow Godot 4 .NET to build the C# project, then open **`res://MAP_CONCEPT/Scene_MapConcept.tscn`**.
2. Press **F6** to run only the map concept. Mouse **wheel** zooms around the cursor, **left/middle/right drag** pans, **R** recentres, and **L** toggles labels.
3. In the upper-left panel, set **Decorative scatter seed** and press **Rescatter**. This changes individual illustrated trees and ground markings but **does not regenerate/relocate the designed coastline, forests, routes, settlements or airstrip**.
4. Press **Export map blueprint JSON** to write the draft coordinate data to `user://map_concept_layout.json`. Godot Output and the panel show its resolved local location. The export is a working blueprint, **not a mesh, raster image or Godot resource imported by WORLD**.

The UI, rendering and data are independently owned: `MapConceptLayout.cs` contains **authored land polygons, named region polygons, site coordinates/IDs, rivers and roads**; `MapConceptArt.cs` renders a simplified illustrative map using CanvasItem drawing; `MapConceptViewer.cs` owns interaction and export; `Scene_MapConcept.tscn` instantiates those scripts. No assets, player, 3D world, shaders or external map addon is required.

## Authoring principles

- **Map first; procedural detail second.** Change coastline/forest/highland/settlement and river/road coordinates in `MapConceptLayout.CreateDefault()` while iterating with the owner. Keep stable IDs when moving existing sites, so future save/terrain references won't break.
- Use illustrated **2D map units** as drafting coordinates, with **X east, Y south**. Exported coordinates map to future Godot world **X and Z** respectively. `MetresPerUnit=4` is merely a provisional scale hint (1600 × 1080 art units ≈6.4 × 4.32 km); **not yet the approved island's playable dimensions**.
- **Keep authored geometry deterministic and distinct from seed-based scatter.** The seed decorates tree drawings and ground marks; do not move the airstrip or regenerate an unrelated shoreline on each seed change.
- The `regions` outline polygons identify **intended types and density hints**, not exact biomes/elevations. They have no pathfinding, true geography, 3D carving, interpolation or terrain heights yet. Forest and mountain icons are illustrations, not separate source meshes/vegetation instances.
- Future integration should use a separately approved map importer/translator that interprets the **raw blueprint** (not art pixels) into island masks, elevation constraints, coast/water, biome/foliage rules and reserved/leveled building footprints. Consider terrain chunks, streaming budgets and stable world coordinates at that stage, **not during BW-MAP1**.
- Simple C# source authoring is the current edit workflow. A full in-editor paint/move/delete UI, multiple named map variants, SVG/PNG export and raster heightmap layers are **possible follow-ups**, not current features.

## Visual acceptance checklist (Godot required)

Opening the scene with F6 should show an island, coastline and surrounding sea, offshore islands, broad forests and mountain ranges, rivers and roads, named sites, a compass rose and labels. Scroll and pan over coast boundaries; verify no input freezes, labels toggle and reset recentres. Change the seed and confirm only decoration changes. Export twice at the same seed and verify identical JSON. Switch back to `SCENE/Scene_World.tscn` and confirm its C# terrain benchmark still runs independently.

**Implementation verification:** Source and path checks only. No local Godot/C# compilation or runtime screenshots have been performed. Any rendering errors, incorrect zoom anchoring or compile issues must be corrected from a local Godot report before treating this preview as accepted.
