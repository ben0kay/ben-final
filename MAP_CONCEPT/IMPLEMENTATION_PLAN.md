# MAP_CONCEPT — Implementation Plan

**Owner goal:** Develop attractive, repeatable 2D island blueprints in an independent Godot F6 scene; use their geometry, elevations and region IDs to inform **future** 3D terrain and biome generation. This plan is scoped to `MAP_CONCEPT/`; it does not authorize changes to `WORLD/` terrain or streaming.

## Roadmap

| Milestone | Scope | Status |
| --- | --- | --- |
| **BW-MAP1** | Numbered authored map preset, zoom/pan, illustration, JSON export | Owner viewed original; numbered pass implemented |
| **BW-MAP2.0** | Seeded coastline foundation and basic controls | Owner tested; too many rounded coastlines |
| **BW-MAP2.0B** | Composite landmasses, peninsulas, deep sea inlets, marching squares; modular `Generation/` and `Rendering/` | Implemented; owner reported map looks OK so far |
| **BW-MAP2.1** | Seed-dependent mountain count/position, coherent inland ridges, foothills, lowland valleys, sampled elevation, contour illustration and export | **Implemented in source; C# build/F6 validation pending** |
| **BW-MAP2.2 — planned** | Inland lakes, freshwater drainage, downhill rivers, coastal estuaries; slope and sea-level checks |
| **BW-MAP2.3 — planned** | Forest zones, meadows, wetlands and biome suitability from elevation/moisture |
| **BW-MAP2.4 — planned** | Variable towns/cities/roads and numbered POIs, placement feasibility and connectivity |
| **BW-MAP2.5 — planned** | Seed-gallery quality checks, biome/land constraints, diagnostics, reproducible saved presets and visual polish |
| **BW-MAP3.0 — planned** | Approved map-data translation into 3D generation, chunk-local sampling and streaming integration only after performance gate |

## MAP2.1 generation contract

- **Coastline stays identical for an existing seed and unchanged MAP2.0B sliders.** MAP2.1 runs after landmass selection with an independent seed stream, not by altering the coastline field.
- Number of mountain ranges is selected deterministically within **min 1 / max 5 by default** (allow 0–8), subject to limited placement attempts. Nearby ranges are separated; candidate ridge paths must be largely inland. Numbered categories remain `Mountains 1`, `Mountains 2`, etc., with IDs such as `mountains_001`.
- Distinct parameters: **Mountain coverage 25%** influences ridge-zone width and symbol density (not exact geographic land coverage); **Ruggedness 60%** influences summit irregularity; **Ridge length 55%** controls range span.
- Sampled `MapConceptElevationGrid` covers 129 × 89 positions in 2D map coordinates, storing normalized **16-bit** elevations, with zero denoting ocean. Lowlands and foothills are preserved even if min=max=0. It is a **concept field**, not a final metric heightmap.
- Actual mountain symbols, region polygons and elevation contours visualize the *same* ridge/elevation data. Old authored map drawings and sites remain unchanged.
- Blueprint schema **5** exports the full seed, control settings, ridge paths, numbered mountain regions and normalized height samples with grid origin/step. No terrain meshes, World generation, rivers, lakes, forests, towns, new gameplay or streaming are introduced.

## Validation checkpoints

1. Build C# and open `MAP_CONCEPT/Scene_MapConcept.tscn` with F6. Inspect a generated seed for connected ranges, higher centres, foothills, valleys and coastlines preserved from MAP2.0B.
2. Use the **same seed and coastline sliders** while changing **only mountain settings**: the coastline must not move. Repeat the same entire configuration and compare ridge IDs, placement and JSON output for determinism.
3. Try min=max=0, min=max=5 and both extremes of coverage/ruggedness/ridge length. Check that the generated number of mountain ranges remains within the requested interval when sufficient inland space exists; report fewer when placement attempts fail.
4. Switch to **Authored Preset**, verify old numbered forests, mountains, rivers, roads and sites persist. No elevation must be inadvertently added to the authored preset.
5. Export blueprint and inspect `schema=5`, `generator_version=3`, `mountain_ridges` and `elevation`. Confirm no changes to `SCENE/Scene_World.tscn` or `WORLD/` runtime code.

**Current validation evidence:** Focused static source/scene checks only; there is no Godot/.NET executable in the implementing environment. F6 screenshots and Godot compile confirmation are needed before marking MAP2.1 accepted.
