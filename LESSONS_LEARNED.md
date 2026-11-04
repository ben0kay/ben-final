# Ben Final — Lessons Learned

Document only significant, reusable mistakes. Preserve an explicit difference between a suspected cause and a locally confirmed cause.

## BW-W1 — Black terrain in first marching-cubes preview (October 30, 2026)

- **Symptom:** `SCENE/Scene_World.tscn` generated nine of nine chunks and about 21,000 triangles, but the terrain appeared completely black rather than grass-green/rock-grey.
- **Investigation:** The original mesher emitted triangles in the reverse order from the classic 256-case lookup table, while the `ArrayMesh` expects a consistent Godot front-face winding. The terrain shader used `cull_disabled`, letting back-facing triangles render. The scene also configured `ambient_light_source = 2` (ambient colour) without an explicit ambient colour; it was not sampling sky ambient illumination.
- **Fix applied:** Emit table triangles in their original order, enable `cull_back` for this single-sided terrain surface, and use `ambient_light_source = 3` (sky). Preserve the indexed mesh, world-space normal gradients, collision and worker queue.
- **Verification required:** Build C# in Godot, run `SCENE/Scene_World.tscn` with F6 and confirm grass/rock coloration, visible top faces, sun response, and no holes between the nine chunks. Also check collider walking and terrain underside once the player is integrated.
- **Prevention:** For each marching-cubes implementation, test a single flat signed-density case with the engine's front-face convention and an upward-looking normal. Inspect ambient-source settings explicitly; a visible sky background doesn't automatically mean the ambient light configuration is correct. Do not hide winding problems with double-sided rendering.
- **Status:** **Suspected root causes; corrected in source, not yet verified visually in Godot.**

## BW-W2.5 — F1 requested size reverted to 27×27 (October 31, 2026)

- **Symptom:** The owner typed `50` in the F1 chunks-per-side field, pressed Regenerate, but the world still showed the 27×27 startup grid.
- **Suspected cause (not confirmed):** The prior Godot `SpinBox` may have displayed uncommitted typed text while the C# callback read its older `.Value` of 27 when the button received focus. The world grid mathematics already supported 50×50.
- **Fix authored:** Replace chunk count and seed SpinBoxes with LineEdit text fields, parse/validate the visible text at button press, show the interpreted NxN count in the panel and log the exact request. Don't silently clamp malformed input to a default. Add explicit confirmation for >50×50 static loads.
- **Prevention:** UI text, committed control values and generation parameters must be cross-checked at the action boundary. Assert test cases including **typing without Enter**, 9→81 chunks, 50→2,500, and invalid text rejected, plus button focus changes.
- **Status:** Source change staged; **awaiting owner Godot runtime confirmation**. Do not call the suspected SpinBox commit behavior the proven cause until verified.

## BW-MAP2.0B — Noisy circles are still circles (November 2, 2026)

- **Symptom:** The owner confirmed MAP2.0's seed-driven coastlines worked but that most mainland shapes still resembled a rounded outward silhouette with noise on its edge. Large ocean basins, deep winding sea inlets and branching land were largely impossible.
- **Confirmed design cause:** The inspected MAP2.0 coastline used a single increasing-angle **polar radius** around the map centre. That representation is star-shaped and cannot represent arbitrary deeply concave/branched land-water topology, no matter how much more shoreline noise is added.
- **Fix implemented in source:** BW-MAP2.0B replaces the polar contour with a continuous land/water field composed from overlapping ellipses and tapered, curved land arms, subtracts ocean-connected curved channels, then extracts coastlines via marching squares. It uses bounded deterministic validation/retries, preserving the original authored preset and full seed. The new sliders control large-scale complexity and ocean penetration separately from fine coastline noise.
- **Prevention:** Decide whether a geometry representation can express the *requested topology* before tuning its random/noise parameters. Separate macro shape composition, geographic constraints and high-frequency detail. Profile on-demand regeneration if grid resolution or candidate retries rise significantly.
- **Verification still needed:** Build .NET, run `MAP_CONCEPT/Scene_MapConcept.tscn` with F6 and compare multiple seeds and extreme slider values; inspect coast continuity, inlets, clipping, island counts and deterministic export. This was source-reviewed only, not proven in Godot.

## BW-MAP2.2A — Verify water connectivity and unify geometry (November 4, 2026)

- **Owner-observed symptom:** F6 screenshots showed river lines stopping short of their ocean inlets and chunky, differently coloured segments, particularly River 1/River 2 near Ocean Inlet 2.
- **Confirmed source causes:** The planner found a distant inland point in a ray fan then blindly appended an inlet tip, without a computed coast intersection. Rendering layered three flat-ended line strokes per raw segment; the 4096 carver used its own unsmoothed centreline. These design differences explain why river art can appear disconnected or blocky. The specific problematic seed is not yet reproduced locally.
- **Source correction:** Find the true sea-to-land boundary from an existing inlet sea point by finite sampling and bisection. Extend a verified river briefly into saltwater; do not emit a river if there is no valid connection. Store parent waterway IDs. Share interpolating path samples for visual rendering and R16/R8 carving, fill with the exact ocean colour, use rounded joins, and avoid the stacked colour bands.
- **Prevention:** Water must be constrained by the *actual exported shoreline and shared channel geometry*. Never use separately generated visual and terrain centreline paths or trust an unvalidated connection endpoint. Treat a requested water feature count separately from the number of valid features created.
- **Verification still needed:** Build Godot C#, run F6 for the owner-reported seed and multiple new seeds; trace each river into the sea, zoom into joins, and compare JSON/R16/R8 export. The new source correction is not yet proven visually.

## BW-MAP2.2A-R2 — Water must shape land, not just match its colour (November 4, 2026)

- **Owner-observed symptom:** Even with matching ocean colour and a connected endpoint, the inland river looked like a line drawn over land. Owner clarified they wanted a gradually narrowing ocean continuation, delta-like branches, an opposite ocean mouth or a connected inland basin.
- **Confirmed design cause:** Ocean inlets were subtracted from the land field before coastline extraction; the river walker ran after mountain/elevation generation and was drawn as a separate stroke. Matching fill colours could not give it the same shoreline, terrain ownership or natural land composition.
- **Source fix:** Construct one sampled sea-level network in the land-shaping stage, extract its banks as ordinary coast, preserve channel-separated land components and generate mountains afterward with shore clearance. Remove the separate procedural river painter/uphill walker. Export explicit coastal semantics and shared junction/rejoin data. R16 bathymetry remains unimplemented and is identified as such.
- **Prevention:** Establish intended water topology and terrain ownership before implementing appearance. A line touching another line is not a shared geographic footprint. Preserve large land fragments when water cuts through a landmass; do not cap them with a decorative-island count.
- **Verification needed:** C# build, owner F6 checks of banks/basin/opposite mouth/branches and mask consistency. Source and diff checks do not establish visual acceptance.


## BW-MAP2.2A-R3 — Route inside actual land; do not design rivers as wide ocean cuts

- **Owner-observed:** Seven True Random screenshots showed mostly broad bays and hollowed mainland centres. Two closer examples still had wide channels and oversized terminal pools.
- **Confirmed source causes:** R2 routed broad analytic paths toward common central distances rather than routing through the selected mainland. Main half-widths 17–24, mouth half-widths up to 115 and pools 45–75 were too large. The 8-unit contour grid discouraged narrowing, and 17-unit bank strokes would obscure narrow channels.
- **Source correction:** Complete/select the mainland first; use bounded mainland-only A* segments through alternating guides, then validate and vector-cut thin rivers. Main half-widths 2.8–4, pools 9–12, short flares only. Share canonical cut polygons for drawing/raster/export; narrow shoreline strokes. Place mountains afterward and keep icons/contours off water.
- **Prevention:** Measure water width relative to land and follow the requested generation order. Do not widen a feature just to fit a coarse grid. Plan routes against actual land and preserve thin bank geometry across render/export stages.
- **Verification:** Source review only. C# build and owner F6 shape approval remain pending; no extensive testing.
