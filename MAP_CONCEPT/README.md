# MAP_CONCEPT — Seeded island cartography

**Status:** MAP1 and MAP2.0B were viewed in Godot by the owner; MAP2.1 mountains also ran in F6 screenshots. **BW-MAP2.1B** introduces a 4096×4096 master elevation blueprint and better world-map inspection; source implemented, **awaiting local Godot/.NET build and F6 visual validation**. No 3D terrain, gameplay or streaming changes.

The phase roadmap, acceptance checklist and future lake/forest/town work are in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

## Run MAP2.1B

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn`, build C# and press **F6**. The independent viewer supports:

- **Procedural Seed:** Generate coastline, numbered mountain ranges, an overview grid and full 4096-square detail layer. The original signed 64-bit seed input and Random Seed button remain.
- **Authored Preset (Island 1):** Original numbered forests/mountains/towns/routes preserved. This preset has no procedural 4096 master until it is explicitly made generatable in a later phase.
- **Generate island from seed:** Runs the concept generator and master rasterization on demand. **Sliders alone never recompute** terrain. Generating the 4096 texture is synchronous and could cause a brief pause, especially on slower PCs.
- **Wheel:** Zoom toward cursor, now up to **32×**. **Drag:** Pan. **R** or Reset map view: fit the actual island and offshore islets into the *available* drawing area. **L** toggles labels. **Hide settings / Show settings:** Gain viewport space; the camera reframes automatically.
- **Export map blueprint JSON:** Writes `user://map_concept_layout.json`; when a procedural master exists, additionally writes `user://map_concept_master_4096.r16` (32 MiB of normalized little-endian 16-bit heights).

Existing sliders for relative landmass size, coastline roughness, peninsula branching, landmass complexity, ocean penetration, offshore islands, mountain count range, coverage, ruggedness and ridge length are unchanged. The MAP2.0B coastline is not re-seeded by mountain-detail logic.

## Three independent dimensions

| Dimension | Current MAP2.1B value | What it means |
| --- | --- | --- |
| **Master elevation data** | **4096 × 4096** | 16,777,216 height samples, 32 MiB raw R16 |
| **Logical world coverage** | **12 × 12 km** | Square master coordinate space; approx. **2.93 m per sample** |
| **Illustrated F6 viewport** | Arbitrary screen/window | Godot vector cartography and cached high-detail relief, with zoom/pan |
| **Broad contour preview** | 129 × 89 | Unchanged inexpensive low-resolution elevation grid for long contours |
| **Current illustration frame** | 1600 × 1080 *map units* | The legacy cartography lives inside the larger square master authoring canvas |

The master uses a **1600 × 1600-unit square**, centred at (0,0), mapping onto 12 × 12 km at 7.5 metres per map unit. Existing coastline/mountain positions remain in the previous centred 1600 × 1080-unit drawing area: the new master adds data resolution and border room without stretching or reshaping the island. Its physical scale is still a **proposed world-design choice**, not a commitment to the final playable island dimensions.

**A 4096 raster does not create 4096×4096 independent mountain features by itself.** MAP2.1B samples the existing broad elevation surface and adds deterministic, higher-frequency details directly at master resolution. A subtle cached RGBA relief image is drawn beneath map symbols so zooming can reveal that added detail. Mountain illustrations are still concept icons, not exact physical peak geometry.

## Future contained mountain authoring

Each `Mountains N` range now has a **local detail seed** and a **bounded region envelope**, independent of its ridge path, the island seed and other ranges' placement. The master evaluates regional microdetail using that range's seed while retaining the broad base-elevation field.

These are deliberately **foundations only**. MAP2.1B does *not* yet add a UI to select a mountain, brush terrain, re-roll one region or export hand-authored edits. Later passes can use the stored bounds and 256×256 raster tile dimensions for local regeneration and per-region overrides rather than overwriting the entire master map.

## Files / ownership

```text
MAP_CONCEPT/
├── Scene_MapConcept.tscn           Independent F6 viewer
├── MapConceptViewer.cs            Camera, UI, generation and export
├── MapConceptGenerator.cs         Coastline -> mountains -> master coordinator
├── MapConceptLayout.cs            Authored/procedural data, schema-6 JSON
├── MapConceptArt.cs               Godot drawing lifecycle / labels
├── Configuration/                 MapConceptMaster.tres settings
├── Generation/
│   ├── MapConceptLandField.cs      FastNoiseLite island composition
│   ├── MapConceptCoastlineContours.cs
│   ├── MapConceptOffshoreIslands.cs
│   ├── MapConceptMountainGenerator.cs
│   ├── MapConceptElevationGrid.cs  Broad 129×89 elevation and ridge IDs
│   ├── MapConceptMasterElevation.cs 4096² R16 and binary export
│   ├── MapConceptMasterGenerator.cs Scanline land mask + microdetail + relief cache
│   ├── MapConceptDetailNoise.cs   Coordinate-stable per-region fine noise
│   ├── MapConceptSeedStream.cs
│   └── MapConceptGeometry.cs
└── Rendering/                     Vector scenery + master relief drawing
```

No new frame-processing scripts or external addons are introduced. The 4096 master is created **once per requested regeneration**, stored as one R16 array, and displayed via one cached RGBA texture (approximately 64 MiB of RGBA8 pixel data before any GPU overhead). Temporary image/export buffers add peak memory use. The master is disposed when the map layout is replaced. Rendering and navigation do not recompute the raster.

## JSON and binary contract

**Schema 6**, **generator version 4**. JSON retains full signed 64-bit world seed as an exact string, coastline and islets, mountain ridge paths and coarse elevation. Additional `master_elevation` metadata describes the **separate** `map_concept_master_4096.r16` file, square resolution, row order, origin/step and proposed world scale. Each `mountain_ridges` entry now carries `local_detail_seed` and `detail_bounds`.

The raw file is **row-major top-to-bottom, 16-bit little-endian normalized heights** (zero ocean; 65535 is maximum). No 16-million-element JSON arrays. The binary file is written only on explicit Export; export both files together. Existing schema-5 exports are not silently treated as high-resolution files.

## Owner F6 acceptance checks

1. Try seeds `254279584798` and `4594759834789` and the previous owner's test seed. Confirm unchanged coast/major mountain arrangement for matching settings, with new fine relief visible when zoomed.
2. Hide the side panel, press **R**, and verify the island is large and centred in the free viewport. Zoom in and out, including mountain zones; ensure panning is smooth without regenerating.
3. Repeat the same seed/settings; compare final relief visually and exported R16 bytes. Change only mountain sliders; coastlines must remain fixed.
4. Export and confirm JSON plus a 33,554,432-byte R16 file. Check schema 6, generation version 4, resolution 4096 and mountain local seeds/region bounds.
5. Restore Authored Preset and confirm unchanged named-by-number features. Check no new errors in Godot Output; confirm `WORLD/` scenes and source are untouched.

**Verification:** Source/path/reference checks only by the implementing agent; no installed Godot/.NET compiler or owner F6 screenshots yet for MAP2.1B. Please report build errors or visual problems before marking accepted.
