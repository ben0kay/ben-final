# MAP_CONCEPT — Seeded island cartography

**Status:** MAP2.2A-R3 replaces R2's broad centre-cut bays with **long, thin, repeatedly winding rivers routed through a completed mainland**. The owner rejected R2 screenshots, including overly wide channels and large pools. R3 is source implemented, awaiting C# build and owner F6 confirmation. Isolated lakes remain MAP2.2B; WORLD/WORLD_CONCEPT are untouched.

## Run and controls

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn`, build C# and press **F6**.

- **Procedural Seed / Generate:** Builds land, connected water corridors, mountains and the 4096 master on demand. Sliders alone do not regenerate.
- **Random seed (same settings):** Picks a new seed and generates with the current slider values.
- **True random (seed + settings):** Randomizes the seed and all geography sliders within their displayed limits, keeps minimum mountains ≤ maximum mountains, then generates once. The resulting settings remain visible/editable and are included in export. Available in Procedural Seed mode; disabled for the authored preset.
- **Authored Preset (Island 1):** Preserves the original numbered regions, routes and sites.
- **Wheel / drag / R / L:** Cursor zoom up to 32×, pan, fit map and toggle labels. Hide settings provides more drawing room.
- **Export:** Writes `user://map_concept_layout.json`, `map_concept_master_4096.r16` (32 MiB) and `map_concept_water_4096.r8` (16 MiB) for a procedural map.

## MAP2.2A-R3 — Mainland, rivers, then mountains

1. Generate and select the **mainland** using only land/coast settings. Water settings no longer influence mainland candidate selection.
2. Build one bounded mainland routing grid. Choose actual shore crossings from that landmass, then route through alternating inland waypoints, using A* to avoid leaving the land. Remove grid loops and smooth the path; reject routes without a sufficiently long inland run or safe channel clearance.
3. Cut the narrow river footprint from the selected land polygons with native vector subtraction. This avoids increasing the entire coastline grid merely to represent a thin channel. Preserve all resulting land components.
4. Add optional offshore islets, then place mountains/elevation and build the 4096 master. Mountain symbols and decorative elevation contours respect final water gaps.

**Width and endings:** Main channels use **2.8–4 map-unit half-widths** (roughly 5.2–8 total after gentle variation), compared with R2's 17–24 half-widths. Mouths flare only over the first/last 65 path units, not across the map. Terminal pools have **9–12 unit radii**, broadening only at the end; the old 45–75-unit pools are removed. These are visual concept dimensions, not calibrated real-world river hydrology. Optional thin anabranches reconnect locally instead of making broad curved cuts.

**Water controls:** Existing Resource fields remain compatible. **Main rivers requested** (`MajorOceanInlets`) remains 1–3. **Inland river reach** adjusts the inland endpoint and the seeded probability of a second ocean outlet (40–75%). Even the low end requests a substantial inland route; zero no longer means a tiny bay. **River meandering** adjusts lateral waypoint displacement while retaining bends. **Ocean mouth widening** (`OceanPenetration`) affects only a short coastal flare. **Maximum joining branches** is an upper bound; invalid routes/branches are skipped, not painted across the ocean. The status counts are the actual features created.

**One shoreline:** After cutting, `FinalShoreline` polygons are canonical. Drawing, master rasterization, land tests and exported coastline use those exact polygons without independently smoothing them again. The procedural beach/ink strokes are narrower so opposing banks cannot fill a thin river. JSON `shoreline_geometry = final_vector_cut` tells consumers not to smooth again. Coarse elevation samples can be wider-spaced than a river, so their decorative contour strokes are suppressed when crossing final water.

**Ownership:** `MapConceptLandField.cs` owns mainland only; `MapConceptGenerator.Finish` owns stage order. `Generation/Water/MapConceptRiverRouter.cs` owns the temporary A* grid, `MapConceptWaterGenerator.cs` creates the river network and `MapConceptWaterCutout.cs` cuts variable-width vector ribbons. `MapConceptWaterFootprint.cs` still excludes optional offshore islets from channels. The obsolete spline water helper is removed. No new processing manager or idle loop is added.

**Limits:** At most eight placement attempts per requested river; no valid long route means fewer rivers rather than fabricated labels over broad sea. Candidate smoothing is clearance-checked; unexpected water-hole topology and cuts removing more than 8% of remaining land per feature are rejected. This is still sea-level connected concept water (R8 class 1), not physically simulated freshwater. R16 zero is sea level/water, not bathymetry; physical vertical scale and independent lakes remain future work. Generation stays synchronous and on demand; runtime timing is unmeasured.

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
├── MapConceptLayout.cs            Authored/procedural data, schema-10 JSON
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

**Current export: Schema 10, generator version 8** (MAP2.2A-R3 mainland-first thin rivers and final vector shores). JSON retains full signed 64-bit world seed as an exact string, coastline and islets, mountain ridge paths and coarse elevation. Additional `master_elevation` metadata describes the **separate** `map_concept_master_4096.r16` file, square resolution, row order, origin/step and proposed world scale. Each `mountain_ridges` entry now carries `local_detail_seed` and `detail_bounds`.

The master elevation is **row-major top-to-bottom, little-endian normalized R16**; the accompanying water mask is **row-major R8** with 0 land / 1 ocean-connected water; 2 and 3 remain reserved. Ocean height is zero. No 16-million-element JSON arrays. The binary files are written only on explicit Export; keep JSON, R16 and R8 together. Existing schema-5 exports are not silently treated as high-resolution files.

## Owner F6 acceptance checks

1. Try seeds `254279584798`, `4594759834789` and the owner screenshot seed. R2 intentionally changes old geography. Follow water from the outer ocean through its narrowing channel; check continuous shoreline banks and connected basin/second mouth, including at deep zoom.
2. Hide the side panel, press **R**, and verify the island is large and centred in the free viewport. Zoom in and out, including mountain zones; ensure panning is smooth without regenerating.
3. Repeat the same seed/settings; compare final relief visually and exported R16 bytes. Change only mountain sliders; coastlines must remain fixed.
4. Export and confirm JSON, a 33,554,432-byte R16 file and a 16,777,216-byte R8 mask. Check **schema 10, generator version 8**, `coastal_channel`, `ending`, `connected_to` and branch `rejoins` IDs. Coastal points are sampled ocean-to-inland corridors, with sea-level surfaces.
5. Restore Authored Preset and confirm unchanged named-by-number features. Check no new errors in Godot Output; confirm `WORLD/` scenes and source are untouched.

**Verification:** The owner confirmed the original MAP2.2A ran in F6 and provided river-gap/colour screenshots. MAP2.2A-R3 has focused source checks only; no installed Godot/.NET compiler was available. Check several seeds, both route endings and extreme settings before marking R3 accepted.

### R3 owner F6 check

Use True Random plus a few fixed seeds; include low reach/high winding and 1/3 requested rivers. Check long, narrow, repeated bends rather than short bays; pools only a little wider than channels; clear mouths and opposite outlets; mountain/contour clearance; retained land on both sides of through-rivers. Export schema 10/version 8 and confirm the R8/zero-R16 channel footprint follows the visible banks. Check both Random buttons still behave as documented, and the authored preset is unchanged. These visual/runtime checks have not been performed by the implementing environment; no extensive automated testing was run.
