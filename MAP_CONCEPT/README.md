# MAP_CONCEPT — Seeded island cartography

**Status:** MAP2.2A-R2 implements the owner's clarified continuous **ocean inlet → narrowing inland channel → connected basin or opposite ocean coast**. The complete water corridor now cuts the land field before coast extraction, mountains and the 4096 master. It is no longer a blue line painted across existing land. **Source implemented; C# compilation and owner F6 validation pending.** Isolated lakes are still MAP2.2B. Authored preset and WORLD runtime are unchanged.

The roadmap and later lake/forest/town work are in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).

## Run and controls

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn`, build C# and press **F6**.

- **Procedural Seed / Generate:** Builds land, connected water corridors, mountains and the 4096 master on demand. Sliders alone do not regenerate.
- **Random seed (same settings):** Picks a new seed and generates with the current slider values.
- **True random (seed + settings):** Randomizes the seed and all geography sliders within their displayed limits, keeps minimum mountains ≤ maximum mountains, then generates once. The resulting settings remain visible/editable and are included in export. Available in Procedural Seed mode; disabled for the authored preset.
- **Authored Preset (Island 1):** Preserves the original numbered regions, routes and sites.
- **Wheel / drag / R / L:** Cursor zoom up to 32×, pan, fit map and toggle labels. Hide settings provides more drawing room.
- **Export:** Writes `user://map_concept_layout.json`, `map_concept_master_4096.r16` (32 MiB) and `map_concept_water_4096.r8` (16 MiB) for a procedural map.

Water controls retain their saved Resource/property names: **Major inlets** 1–3; **Inland channel reach** 0–100%; **Channel meandering** 0–100%; **Maximum joining branches** 0–6. Ocean penetration affects the estuary taper and bend. Reach increases basin travel inland; at ≥55%, a seeded 35% choice can instead create an ocean-to-ocean passage. These choices are repeatable, not a guarantee every seed contains both endings. Changing water settings intentionally reshapes the island and can move its mountain ranges. Only mountain-detail changes preserve the coast.

## MAP2.2A-R2 — Water shapes the geography

One sampled corridor defines the broad estuary, narrowing inland channel and its widening basin or second ocean mouth. Inlet and river records share the same junction position, tangent and width; splitting records only provides labels and IDs. Optional delta-like anabranches leave the inlet and rejoin its inland channel. Their endpoints are shared with the trunk. Branches can merge with one another or other water corridors.

The full network is subtracted from the land field **before** extracting coastlines. Its banks use the exact same coastal apron and ink outlines as the ocean. Separate land components created by through-passages and delta branches are retained even when the optional offshore-island target is zero. The status therefore reports **other land components**, not a promise that this count equals the offshore target.

Mountains are generated on the resulting land; ridge placement checks intermediate points and distance from each containing land component's shores. Mountain icons require clearance for their full footprint. Coarse elevation, master raster and water mask use the resulting island boundaries. There is no procedural blue river overlay and no second freshwater carve.

**Water meaning:** These channels and connected basins are all sea-level, ocean-connected water, exported as **R8 class 1**. They are conceptual estuaries/straits, not a freshwater drainage simulation. A basin's planned endpoint is pulled toward the central land core if there is insufficient room. Other inlets can merge with it, so `ending` describes the constructed route, not a guarantee that a basin remains a separate lobe. Separate isolated lakes, freshwater flow, underwater bed depths and physical vertical scale remain future work. R16 zero currently marks sea level/water, not bathymetry.

**Ownership:** `Generation/Water/MapConceptWaterGenerator.cs` builds the network, `MapConceptWaterFootprint.cs` indexes its sampled widths for land-field queries, and `MapConceptWaterway.cs` owns typed metadata. `MapConceptLandField.cs` cuts the geographic channels; coast extraction and normal island rendering provide their banks. `MapConceptWaterCarver.cs` now classifies the final raster only. The old procedural stroke painter and uphill river walker have been removed.

The spatial index bounds corridor queries during generation. All work remains on demand; no live processing manager is added. Master generation remains synchronous and should be checked for acceptable regeneration time on the owner's PC.

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
├── MapConceptLayout.cs            Authored/procedural data, schema-9 JSON
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

**Current export: Schema 9, generator version 7** (MAP2.2A-R2 geographic coastal corridors). JSON retains full signed 64-bit world seed as an exact string, coastline and islets, mountain ridge paths and coarse elevation. Additional `master_elevation` metadata describes the **separate** `map_concept_master_4096.r16` file, square resolution, row order, origin/step and proposed world scale. Each `mountain_ridges` entry now carries `local_detail_seed` and `detail_bounds`.

The master elevation is **row-major top-to-bottom, little-endian normalized R16**; the accompanying water mask is **row-major R8** with 0 land / 1 ocean-connected water; 2 and 3 remain reserved. Ocean height is zero. No 16-million-element JSON arrays. The binary files are written only on explicit Export; keep JSON, R16 and R8 together. Existing schema-5 exports are not silently treated as high-resolution files.

## Owner F6 acceptance checks

1. Try seeds `254279584798`, `4594759834789` and the owner screenshot seed. R2 intentionally changes old geography. Follow water from the outer ocean through its narrowing channel; check continuous shoreline banks and connected basin/second mouth, including at deep zoom.
2. Hide the side panel, press **R**, and verify the island is large and centred in the free viewport. Zoom in and out, including mountain zones; ensure panning is smooth without regenerating.
3. Repeat the same seed/settings; compare final relief visually and exported R16 bytes. Change only mountain sliders; coastlines must remain fixed.
4. Export and confirm JSON, a 33,554,432-byte R16 file and a 16,777,216-byte R8 mask. Check **schema 9, generator version 7**, `coastal_channel`, `ending`, `connected_to` and branch `rejoins` IDs. Coastal points are sampled ocean-to-inland corridors, with sea-level surfaces.
5. Restore Authored Preset and confirm unchanged named-by-number features. Check no new errors in Godot Output; confirm `WORLD/` scenes and source are untouched.

**Verification:** The owner confirmed the original MAP2.2A ran in F6 and provided river-gap/colour screenshots. MAP2.2A-R2 has focused source checks only; no installed Godot/.NET compiler was available. Check several seeds, both route endings and extreme settings before marking R2 accepted.

### R2 visual acceptance

Check 1 and 3 inlets, 0/100 reach and winding, 0/6 branches, and zero optional offshore islands. Through-channels must preserve both land halves; delta islands must not disappear merely because the optional island target is zero. Check mountain icons and contour strokes do not cover the water. The exported R8 ocean footprint and R16 zero-water footprint must match the illustrated shoreline, allowing raster/antialiasing edge differences. Switch to the authored preset and back. Exported paths are sampled corridor data; consumers must not smooth them again. Coastline polygons remain authoritative after contour sampling/simplification and drawing smoothing. If contour extraction fails completely, the pre-existing safe elliptical fallback contains no waterways; the status count reveals that case.
