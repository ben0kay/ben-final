# MAP_CONCEPT — Implementation Plan

**Owner goal:** Develop attractive, repeatable 2D island blueprints in an independent Godot F6 scene; use their geometry, elevations and region IDs to inform **future** 3D terrain and biome generation. This plan is scoped to `MAP_CONCEPT/`; it does not authorize changes to `WORLD/` terrain or streaming.

## Roadmap

| Milestone | Scope | Status |
| --- | --- | --- |
| **BW-MAP1** | Numbered authored map preset, zoom/pan, illustration, JSON export | Owner viewed original; numbered pass implemented |
| **BW-MAP2.0** | Seeded coastline foundation and basic controls | Owner tested; too many rounded coastlines |
| **BW-MAP2.0B** | Composite landmasses, peninsulas, deep sea inlets, marching squares; modular `Generation/` and `Rendering/` | Implemented; owner reported map looks OK so far |
| **BW-MAP2.1** | Seed-dependent mountain count/position, coherent inland ridges, foothills, lowland valleys, sampled elevation, contour illustration and export | **Owner viewed F6 mountain map; tuning ongoing** |
| **BW-MAP2.1B** | **4096×4096 master elevation** on a proposed 12×12 km square map, high-detail shaded-relief image, more usable framing/zoom, bounded mountain-local detail seeds, R16+JSON export | **Implemented in source; Godot C#/F6 validation pending** |
| **BW-MAP2.2A** | Sea inlets, freshwater rivers, tributaries, carving and water mask | **Owner tested F6; disconnected rivers and blocky drawing found** |
| **BW-MAP2.2A-R1** | Coast-validated river outlets, shared smoothed carve/render path, ocean-matched colouring and waterway connection IDs | **Source correction pushed; awaiting F6 validation** |
| **BW-MAP2.2B** | Standalone inland lakes, overflow and wetlands | **Planned** |
| **BW-MAP2.3 — planned** | Forest zones, meadows, wetlands and biome suitability from elevation/moisture |
| **BW-MAP2.4 — planned** | Variable towns/cities/roads and numbered POIs, placement feasibility and connectivity |
| **BW-MAP2.5 — planned** | Seed-gallery quality checks, biome/land constraints, diagnostics, reproducible saved presets and visual polish |
| **BW-MAP3.0 — planned** | Approved map-data translation into 3D generation, chunk-local sampling and streaming integration only after performance gate |

## Intended handoff: MAP_CONCEPT → WORLD_CONCEPT (planned)

- **MAP_CONCEPT is for 2D layout experimentation and approval.** The owner will iterate on island shape, mountain regions, forests, lakes, rivers, settlements and their relative positions here first. It remains an independently runnable concept tool, not the playable world.
- **WORLD_CONCEPT is a future separate folder/test environment**, created only when the owner is happy with a chosen map layout. It will consume the approved exported blueprint/data (not merely a screenshot) and translate its coastline, elevation and region information into actual Godot 3D terrain, water and vegetation for free-fly visual inspection.
- **Iterate visually in 3D:** fly around the generated mountains, forests, lakes and other regions; refine map data or translation rules as necessary without losing the selected layout. Preserve deterministic seeds, stable coordinates and versioned map exports.
- **Then investigate world generation and streaming:** chunk generation/loading/unloading, distant terrain/LOD, vegetation residency and performance are subsequent experiments, not prerequisites to using MAP_CONCEPT. Coordinate this work with the existing `WORLD/` implementation and streaming plans rather than silently replacing them.
- **Status: design intention only.** Do not create `WORLD_CONCEPT/`, alter `WORLD/`, or implement map-to-3D integration until explicitly requested. The eventual folder structure and integration milestone can be decided then.

## MAP2.1 generation contract

- **Coastline stays identical for an existing seed and unchanged MAP2.0B sliders.** MAP2.1 runs after landmass selection with an independent seed stream, not by altering the coastline field.
- Number of mountain ranges is selected deterministically within **min 1 / max 5 by default** (allow 0–8), subject to limited placement attempts. Nearby ranges are separated; candidate ridge paths must be largely inland. Numbered categories remain `Mountains 1`, `Mountains 2`, etc., with IDs such as `mountains_001`.
- Distinct parameters: **Mountain coverage 25%** influences ridge-zone width and symbol density (not exact geographic land coverage); **Ruggedness 60%** influences summit irregularity; **Ridge length 55%** controls range span.
- Sampled `MapConceptElevationGrid` covers 129 × 89 positions in 2D map coordinates, storing normalized **16-bit** elevations, with zero denoting ocean. Lowlands and foothills are preserved even if min=max=0. It is a **concept field**, not a final metric heightmap.
- Actual mountain symbols, region polygons and elevation contours visualize the *same* ridge/elevation data. Old authored map drawings and sites remain unchanged.
- Blueprint schema **5** exports the full seed, control settings, ridge paths, numbered mountain regions and normalized height samples with grid origin/step. No terrain meshes, World generation, rivers, lakes, forests, towns, new gameplay or streaming are introduced.

## MAP2.1B master-map contract

- The **4096×4096 master raster** covers a **12×12 km** square conceptual world at roughly **2.93 m/sample**. It uses normalized `ushort` heights (32 MiB); the existing 129×89 field remains as the fast broad-shape/contour reference.
- Preserve the previous **coastline and primary mountain ridge positions** for identical seed/settings; build the full-resolution heightmap **after** those layout decisions. Higher-frequency detail is sampled in continuous map coordinates (not indexed by screen pixels), so pan/zoom do not regenerate terrain.
- Render the master through **one cached 4096² RGBA relief texture**, below existing mountain symbols. Allow up to **32× zoom** and fit the island within the free viewport to the right of the settings panel. The panel can collapse. No live slider recomputation, per-frame simulation, or continuous texture upload.
- Individual ranges retain `mountains_NNN` IDs plus independent **local detail seeds and bounds**. Future local edits should target 256² tiles that intersect those bounded regions, retaining unrelated mountain geometry and coastline shape. Tile size is currently metadata, **not a completed tile regeneration/editing system**.
- Blueprint **schema 6**, generator **version 4**. JSON records the 4096 master metadata and local ridge seeds, not the full 16M pixels. Explicit export also writes `user://map_concept_master_4096.r16`, little-endian row-major 16-bit normalized data (32 MiB). Raw binary export is not performed on every seed change.
- The existing illustrated geometry still spans a centred 1600×1080-unit region inside the square 1600×1600-unit master canvas (7.5 m/unit). A bigger data canvas **does not itself increase the underlying island footprint**; framing changes make the island fill more of the viewport. Expanding the actual shoreline/world-area generator is a separate design decision.
- Synchronous on-demand generation may pause the F6 viewer and temporarily allocate more than 100 MiB (heights + RGBA pixels + image/upload/export buffers). Watch real-generation duration on the owner's PC. Optimize or tile generation only if measurements warrant it.

### MAP2.1B acceptance

1. Open F6; confirm the displayed master is 4096² and the coastal layout/ridge placements match old seeds when settings match. Verify high-res shaded relief is visible at close zoom and no flat, rectangular opaque overlay covers the sea.
2. Hide panel, press R, confirm the island occupies the available canvas. Show panel and repeat; mouse pan and wheel zoom should not regenerate or upload data.
3. Verify same seed, mountain settings and generator version produce identical binary R16 output. Test different mountain local seeds later without altering coastline; **per-mountain reroll UI is not implemented yet**.
4. Export schema-6 JSON plus the `4096²×2 = 33,554,432`-byte R16 file. Ensure metadata's coordinate origin, map span, sample format, height order, and relative path are correct.
5. Switch to Authored Preset. Check unchanged landscapes/labels, no compile errors and no modifications to actual `WORLD/` runtime code.

## Validation checkpoints

1. Build C# and open `MAP_CONCEPT/Scene_MapConcept.tscn` with F6. Inspect a generated seed for connected ranges, higher centres, foothills, valleys and coastlines preserved from MAP2.0B.
2. Use the **same seed and coastline sliders** while changing **only mountain settings**: the coastline must not move. Repeat the same entire configuration and compare ridge IDs, placement and JSON output for determinism.
3. Try min=max=0, min=max=5 and both extremes of coverage/ruggedness/ridge length. Check that the generated number of mountain ranges remains within the requested interval when sufficient inland space exists; report fewer when placement attempts fail.
4. Switch to **Authored Preset**, verify old numbered forests, mountains, rivers, roads and sites persist. No elevation must be inadvertently added to the authored preset.
5. Export blueprint and inspect `schema=5`, `generator_version=3`, `mountain_ridges` and `elevation`. Confirm no changes to `SCENE/Scene_World.tscn` or `WORLD/` runtime code.

**Current validation evidence:** Focused static source/scene checks only; there is no Godot/.NET executable in the implementing environment. F6 screenshots and Godot compile confirmation are needed before marking MAP2.1 accepted.

## MAP2.2A water system contract

This milestone lives inside `MAP_CONCEPT/`; it does not modify the 3D `WORLD/` prototype or planned `WORLD_CONCEPT/` handoff. The existing ocean-carving paths are retained as typed, numbered inlet features. Each generated island requests 1–3 major ocean-connected inlets (default 1). A separately seeded, bounded elevation-guided path generator attempts a freshwater continuation inland and optional joining tributaries. The main water route is stored source-to-mouth, including widths and a nonincreasing downstream water-level profile. The algorithm is an approximate valley walker, **not** a watershed or erosion simulator; unsuitable geography can result in shorter or missing rivers.

New F6 controls: major inlets (1–3), freshwater reach (0–100%), river winding (0–100%), and maximum tributaries (0–6). The existing ocean penetration slider still sets inlet depth. These settings update only on explicit generation. Mountain creation remains a separate stage.

The 4096 master carves freshwater channels and blends their banks locally. It stores a separate **R8 water mask** (0 land, 1 ocean, 2 main river, 3 tributary). **Original MAP2.2A schema 7, generator version 5** exported waterways, path widths and normalized surface levels. **MAP2.2A-R1 is schema 8, generator version 6**, adding `connected_to` parent-waterway IDs and coast-validated routes. Explicit Export writes JSON, a 32-MiB R16 heightmap and a 16-MiB R8 water mask. MAP2.2B will add separate inland lakes and their outlets later.

**F6 acceptance:** Test multiple seeds, especially 0/100% Ocean Penetration, River Reach and Winding. Check inland connection, river/tributary joins, no obvious uphill reversals, correct river carving and same-seed reproducibility. Check the 3 export files and Authored Preset. Coastline output may change from previous versions because guaranteed inlets alter the land mask. This source pass is not yet runtime-validated.

## MAP2.2A-R1 — User-reported river discontinuities (November 4, 2026)

- **Observed:** Owner screenshots show separate blue river blocks, with River 1/River 2 apparently not reaching Ocean Inlet 2; water tint differs sharply from the ocean. This is a MAP2.2A regression and must be resolved **before** starting MAP2.2B.
- **Code finding:** The river-mouth finder chose an arbitrary inland point in a forward fan and appended the original inlet tip; it did not explicitly detect the shoreline crossing. The visual painter layered three wide, flat-ended strokes per segment while master carving used raw linear segments.
- **Fix (source):** Scan out from an actual inlet sea point to the first mainland shore, bisect the real land/water boundary, then start upstream inland. Complete the waterway via the true shore and a short sea overlap. If no valid coast crossing exists, skip the river instead of rendering a detached feature. Main/tributary records export their parent waterway IDs. Use common Catmull-Rom centreline samples for the painter and raster carver, same ocean RGB and rounded water caps. Terrain export format remains R16 + R8.
- **Validation needed:** Rebuild F6. Revisit the owner-provided disconnected River 2 seed if saved; test 1 and 3 ocean inlets, short/long river reach, high winding and several random seeds. For *each* displayed river, follow its downstream water path and verify it intersects its matching ocean inlet with no land-coloured gap. Zoom in to inspect smooth edges and matching sea blue; compare vector channels with R8 carving. Confirm no river is emitted for an unreachable inlet, no detached tributary ends, export schema 8/version 6 and JSON `connected_to` values. This remains a bounded elevation-guided river planner, not a full global hydrology simulation.
