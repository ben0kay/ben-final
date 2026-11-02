# MAP_CONCEPT — Seeded island cartography

**Status:** The owner tested the first authored map visually and confirmed MAP2.0's coastline-only islands looked acceptable but too consistently round. **BW-MAP2.0B** replaces radial coastlines with a 2D land/water field; **source implemented, pending Godot F6 visual validation**. This folder remains independent of WORLD, gameplay, terrain streaming and War Machine.

## Run MAP2.0B

Open `res://MAP_CONCEPT/Scene_MapConcept.tscn` in Godot 4 .NET and press **F6**. Select **Procedural Seed** (default) for seeded coastlines or **Authored Preset (Island 1)** to restore the original numbered forests, mountains, towns, rivers and roads. The authored layout is untouched.

Use a signed 64-bit **World seed** (e.g. `254279584798` or `4594759834789`), or press **Random seed and regenerate**. Enter/Generate applies the seed and all sliders. Zoom with mouse wheel, pan by dragging, **R** resets view, **L** shows/hides labels, and the side panel scrolls.

| Control | Effect |
| --- | --- |
| **Landmass size (35–75)** | Relative size of the large landforms, not an exact land-percent target |
| **Landmass complexity (0–100)** | Number/variation of overlapping land regions, peninsula arms and large-scale shoreline deformation |
| **Ocean penetration (0–100)** | How deeply curved, ocean-connected sea inlets enter the land; near zero suppresses them |
| **Peninsula branching (0–100)** | Length/number/width of irregular connected land arms |
| **Coastline roughness (0–100)** | Secondary shore detail and noise warping, rather than controlling the entire silhouette |
| **Offshore islands (0–8)** | Requested maximum small islands; may be fewer where sea space and clearance disallow them |

The map status displays **actual computed land area** and placed island count. Changing the seed now affects entire coastlines, not merely slightly different ripples on an ellipse. Different seeds may yield compact, extended, multi-armed or heavily indented landmasses, depending on the sliders. There is no promise that every single seed will produce a perfect coastline—this is an experimental generator intended for visual iteration.

**Ocean-connected inlets are saltwater**, sometimes narrowing enough to resemble tidal rivers. Proper *freshwater* rivers, inland lakes, drainage basins and watershed simulation are **future MAP2.1+ tasks**, not part of MAP2.0B. Similarly, procedural mode still has **no generated mountains, forests, towns/cities, POIs or roads**.

## How the generator works

1. A deterministic SplitMix64 stream derives independent random values and 32-bit FastNoiseLite seeds from the **full 64-bit input**.
2. One large mainland core is connected to **overlapping irregular ellipses and curved/tapered land arms**. This is deliberately *not* a polar radius around a single centre.
3. A small bounded number of **curved sea channels** is carved from ocean to inland. Mouths are wide, with optional inland basins and narrower ends. Domain warp and layered noise perturb the larger land forms.
4. A finite **8-map-unit land/water grid** is sampled. A local marching-squares implementation interpolates crossings, resolves ambiguous diagonal cases, traces closed land contours, and simplifies edges before rendering.
5. The **largest viable land contour** is the mainland. A bounded deterministic validation/retry pass favours a useful area, two-dimensional extent and dominant main island. Split-off fragments and optional additional offshore islands are bounded by the island-count setting. Invalid contours and internal water holes are excluded until proper lake support.
6. The existing `MapConceptArt` renders the resulting polygons without knowing whether the data came from the authored preset or generator. Regeneration occurs **on demand**, not continuously while exploring the map.

**Known limitations:** This is a 2D concept-quality coastline model, not a guarantee of physically realistic geography. Narrow cut-throughs depend on grid resolution. Very small islands or canals may disappear when simplified, the validation may select a later deterministic attempt, deep sea cuts can detach peninsulas, and inland water holes are not rendered as lakes. We can tune shape distributions after inspecting multiple F6 screenshots. The small density grid and contour data are meant for **map concept authoring**, not directly for 3D terrain, physics or streaming.

## Files and data

- `MapConceptGenerator.cs`: non-radial land/water density, FastNoiseLite, sea cuts, marching squares, contour validation, offshore islands.
- `Configuration/MapConceptSettings.cs` and `MapConceptMaster.tres`: default parameters and adjustable bounds.
- `MapConceptLayout.cs`: both layout modes, numbered authored region/POI IDs and JSON blueprint.
- `MapConceptArt.cs`: shared illustrated map rendering.
- `MapConceptViewer.cs` and `Scene_MapConcept.tscn`: F6 input, sliders, camera, export and labels.

The **Export map blueprint JSON** button writes the currently displayed layout to `user://map_concept_layout.json`. **Schema 4** includes `layout_mode`, exact `world_seed` as a decimal string, generator version **2**, sliders including `landmass_complexity` and `ocean_penetration`, land polygons and optional features. Earlier schema versions are only draft exports. These are **2D illustrative coordinates** (X east, Y south), and `MetresPerUnit=4` is merely a provisional scale hint.

The 3D translation and the BW-W4 streaming programme are **still separate and unchanged**.

## Short owner test (Godot required)

1. Build C#, press F6, regenerate `254279584798` and `4594759834789` with the same controls; confirm the landmasses vary in overall structure and do not clip the map frame.
2. Try **Ocean Penetration 0 vs 100**, then **Landmass Complexity 0 vs 100** and **Peninsula Branching 0 vs 100**, using the same seed. Note which coastlines look interesting and which look unnatural.
3. Regenerate the same seed/settings twice and verify identical coastlines. Verify island counts and no unbounded retries/frozen frame.
4. Switch to **Authored Preset**, confirm original forests, mountains, towns and roads remain intact, then return to Procedural.
5. Export identical settings twice, compare JSON, and confirm `world_seed`, `generator_version=2`, `schema=4`. Check zoom, pan, UI scrolling.

**Verification performed by agent:** focused source/scene checks only; no installed Godot/.NET compiler or actual generated-map screenshots were available. Mark visual quality as *awaiting owner validation*.
