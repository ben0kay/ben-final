# MAP_CONCEPT — Seeded island cartography

**BW-MAP1:** The original numbered island concept was visually inspected by the owner in Godot. **BW-MAP2.0:** Seed-driven coastline generator implemented in source; Godot/.NET build and F6 appearance await local validation. This folder is isolated from 3D WORLD terrain, streaming and War Machine.

## Run and controls

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn` in Godot 4 .NET and press **F6**.

- **Map source:** Select **Procedural Seed** (default) or **Authored Preset (Island 1)**. The original Island 1 retains its numbered forests/mountains/towns/roads; it is never overwritten.
- **World seed:** Enter any valid signed 64-bit integer, including `254279584798` or `4594759834789`, in the **text field** and press Enter or **Generate island from seed**. Random Seed generates a new long seed and redraws.
- **Landmass size target:** 35–75%, approximate relative control. The status panel shows the *actual polygon-area fraction*; large peninsulas and the map-edge safety margin can make it differ from the target.
- **Coastline roughness:** 0–100% adjusts broad/detail noise.
- **Bays and peninsulas:** 0–100% adjusts macro lobes and warp.
- **Offshore islands:** Request 0–8. The bounded placement algorithm may produce fewer if the available sea has insufficient clearance; the status displays actual/requested.
- **Mouse wheel** zooms under cursor; **left/middle/right drag** pans; **R** resets; **L** toggles labels. Scroll the side panel to see all controls.
- **Export map blueprint JSON** writes the currently displayed layout and its settings to `user://map_concept_layout.json`; the panel displays the OS path.

**Procedural mode generates coastline only.** Forests, mountains, rivers, lakes, roads, settlements and other POIs are intentionally *absent* until MAP2.1–2.3, not carried over from the authored preset. Changing seeds in **Authored Preset** affects only decorative marks, not geography.

## Ownership and generation

| File | Role |
| --- | --- |
| `Scene_MapConcept.tscn` | Independent F6 2D viewer; scrollable slider UI |
| `Configuration/MapConceptSettings.cs` and `Configuration/MapConceptMaster.tres` | Validated defaults for seed/shape controls |
| `MapConceptGenerator.cs` | FastNoiseLite noise layers, full-seed-derived streams, island contour and bounded offshore-island placement |
| `MapConceptLayout.cs` | Original authored preset, generated-coastline factory, stable numbered feature IDs and JSON export |
| `MapConceptArt.cs` | Procedural/artwork rendering and reusable labels |
| `MapConceptViewer.cs` | 64-bit seed input, mode selection, sliders, navigation and export |

The generator uses full-length signed `long` seed values. A deterministic SplitMix64 stream derives **32-bit FastNoiseLite sub-seeds** and shape parameters, avoiding seed truncation. A radial contour with positive radius and monotonically ordered angles ensures **one connected, non-self-intersecting main island**. The shore is kept inside an inset rectangular safe zone. Fine coastline noise, macro bays and island rotation/axis ratio provide variety. Offshore islands are small bounded polygons, placed outside the coast with minimum clearance from mainland and from other islands.

This deliberately constrained first algorithm is *star-shaped*; extreme fjords, true archipelagos, more elaborate coastline erosion, hydrology and geographically valid biome/city placement are subsequent experiments. Slider percentages are not mathematical guarantees of visual quality. Regeneration is on-demand (not per-frame), and no procedural resources are built while merely panning/zooming.

## Export schema and future terrain

Schema **3** includes `layout_mode`, exact `world_seed` **as a string**, `generator_version`, `generation_settings`, `coastline`, `islets`, and existing `regions`, `routes`, `sites` lists (empty for the current procedural mode). Older schema-1/2 files are drafts. The coordinates remain **2D illustrative units** (X east, Y south; eventual world Z). The existing 4-metres-per-unit hint is **not** an approved playable island size. Do not mistake these polygons for a terrain heightmap or streamed 3D chunks.

## Owner's F6 check

1. With identical sliders, generate `254279584798` and `4594759834789`; verify significantly different coastlines and intact sea.
2. Regenerate each seed twice; the coastline/islets should match exactly. Change each slider and check that land stays inside the border.
3. Switch to **Authored Preset**; verify its original numbered forests, mountains, towns, routes and icons remain intact. Switch back and check procedural re-generation.
4. Export at the same seed/settings twice; JSON should match, and its exact seed string and generator version should be present. Check mouse panning, zoom, labels, panel scrolling.
5. Confirm `SCENE/Scene_World.tscn` remains unchanged.

**Verification:** Source and scene-reference checks only by the implementing agent; no Godot or .NET runtime was available. The owner's earlier screenshot establishes MAP1's visible scene, not MAP2.0's visual quality.
