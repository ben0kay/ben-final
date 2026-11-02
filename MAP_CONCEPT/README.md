# MAP_CONCEPT — Seeded island cartography

**Status:** MAP1 authored map and MAP2.0B coastline generator have been owner-tested visually. **BW-MAP2.1** mountain/elevation code is implemented but requires a local Godot C# build and F6 check. The concept remains separate from `WORLD/` terrain, gameplay and streaming.

See **[IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md)** for the milestone roadmap, acceptance checks and future lake/river/forest/town work.

## F6 usage

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn`, build C# and press **F6**.

Choose **Procedural Seed** for seed-dependent coastlines, mountain ranges and an elevation preview; choose **Authored Preset (Island 1)** for the original unmodified numbered map. Enter a full signed 64-bit seed (e.g. `254279584798`) and press Enter or **Generate island from seed**, or use **Random seed and regenerate**. Scroll wheel zooms, drag pans, **R** resets and **L** toggles labels.

### Map generation sliders

| Slider | Default | Behaviour |
| --- | ---: | --- |
| Landmass size | 60 | Relative landmass size; not exact land percentage |
| Coastline roughness | 45 | Fine edge and domain-warp detail |
| Peninsula branching | 55 | Connected land arms |
| Landmass complexity | 65 | Broad land-region shape variation |
| Ocean penetration | 65 | Ocean-connected inland bays/inlets |
| Offshore islands | 2 | Requested maximum; placement may be lower |
| **Minimum mountain ranges** | **1** | Lower bound on seeded range count (0–8) |
| **Maximum mountain ranges** | **5** | Upper bound on count (0–8); UI keeps max ≥ min |
| **Mountain coverage** | **25** | Affects mountain zone width and drawing density; approximate influence, not exact fraction |
| **Mountain ruggedness** | **60** | Varies summit irregularity |
| **Ridge length** | **55** | Adjusts long connected mountain chains |

The full seed and sliders determine the same map on repeated regeneration. **Mountain settings do not participate in coastline generation**, so changing them should preserve the previously chosen shoreline. Mountains favour inland areas and are kept apart by placement rules. On severely cut or very narrow coastlines the bounded placement algorithm may produce fewer ranges than the requested random count; the status panel shows the count actually placed.

## What MAP2.1 adds

The generator first creates the MAP2.0B landmass using a 2D land/water field, overlapping land regions, curved peninsula arms, FastNoiseLite, sea-inlet carving and marching-squares contours. **After the coast has been selected**, MAP2.1 independently generates zero to several long, winding mountain centreline paths and numbered highland regions. An elevation sampler builds a **129 × 89 normalized 16-bit grid**:

- **Ocean:** zero; lowland height rises gradually inland from the coast.
- **Hills/valleys:** lower-amplitude seeded noise maintains uneven lowland geography.
- **Mountain ranges:** higher central ridges fade into foothills with distance, modulated by ruggedness.
- **Drawing:** the existing illustrator uses the generated highland polygons for mountain symbols and subtle topographic contour strokes from the sampled elevation. A range is **more than a collection of random symbols**: its centreline drives the altitude field.

The generated elevation is a 2D concept field with relative values in `[0,1]`, **not actual terrain metres**. It is sampled on demand only when regenerating the map, not every frame. The F6 view draws and exports this data without creating new 3D meshes, colliders or streaming jobs. The old authored preset remains intact and has no attached elevation field.

**Not in MAP2.1:** lakes, freshwater rivers, drainage, forests, wetlands, cities, towns, roads or POI placement. Ocean-connected inlets are saltwater. These systems will depend on the elevation field in future milestones.

## Folder ownership

```text
MAP_CONCEPT/
├── Scene_MapConcept.tscn
├── MapConceptViewer.cs
├── MapConceptArt.cs
├── MapConceptLayout.cs
├── MapConceptGenerator.cs
├── IMPLEMENTATION_PLAN.md
├── Configuration/
│   ├── MapConceptSettings.cs
│   └── MapConceptMaster.tres
├── Generation/
│   ├── MapConceptLandField.cs
│   ├── MapConceptCoastlineContours.cs
│   ├── MapConceptOffshoreIslands.cs
│   ├── MapConceptSeedStream.cs
│   ├── MapConceptGeometry.cs
│   ├── MapConceptMountainGenerator.cs
│   └── MapConceptElevationGrid.cs
└── Rendering/
    ├── MapConceptNaturalPainter.cs
    ├── MapConceptFeaturePainter.cs
    ├── MapConceptDrawingGeometry.cs
    └── MapConceptElevationPainter.cs
```

Rendering classes remain plain C# helpers rather than new scene nodes. Map generation and painting are activated only by initial load and regeneration; zoom/pan does not recompute elevation. Existing `MapConceptViewer` and `MapConceptArt` scene paths remain unchanged.

## JSON export and future 3D usage

**Export map blueprint JSON** writes `user://map_concept_layout.json`. Schema **5** adds `mountain_ridges` (numbered stable IDs, centerlines, width and strength) and `elevation` (columns, rows, 2D origin, step, row-major `u16_normalized` heights) to existing coastlines/regions/islands. The full 64-bit `world_seed` remains an exact string. `generator_version=3` distinguishes the new sampled-elevation output from MAP2.0B's version 2.

Coordinates are still illustrative map units (X east, Y south; future world Z); the 4-metres-per-unit hint is not a finalized island size. A later importer must translate this validated blueprint into chunk-friendly terrain heights and slopes and separately generate caves, foliage and gameplay objects. **Nothing in `WORLD/` is connected to this yet.**

**Local verification needed:** C# build; F6 for multiple seeds; changing mountain sliders without moving coastline; same-seed repeatability; authored preset restoration; JSON export; no red errors or clipped/crossing overlays. Source/scene checks do not prove the visual quality of the results.
