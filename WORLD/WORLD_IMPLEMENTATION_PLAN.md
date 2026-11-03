# WORLD Implementation Plan — Ben Final

> **Purpose:** Working technical handoff for Codex and future AI agents. Read before changing `WORLD/` code. This is a **plan and test record**, not authorization to implement every stage.
>
> **Repository:** `ben0kay/ben-final` — current development branch `main`.
> **Last documented user tests:** October 30, 2026 (Godot 4.7.2 .NET, screenshots supplied in chat).
> **State:** The owner ran **27×27 = 729 chunks (1,728 m)** in Godot and found the lowlands bland and the mountains a continuous wall of thin spikes. The F1 panel now accepts **1–100 chunks per side**, any integer, and exposes seven terrain sliders plus seed and mesh detail; the revised terrain/input awaits local validation. **New priority:** establish genuinely smooth terrain streaming, then opt-in disposable tree/grass/prop/dummy-entity stress tests **before any War Machine gameplay port**. Streaming, mixed LOD, water, caves, vegetation systems, and persistence remain **unimplemented**.
>
> **Companion documents:** [WORLD_AUTHORING.md](WORLD_AUTHORING.md) explains the intended geology/terrain techniques; [README.md](README.md) explains existing files and how to run them; [root README](../README.md) tracks the project's milestone statuses; [CodePreferences](../CodePreferences.md) and [AGENTS](../AGENTS.md) govern coding and Git practice.

## Independent map-first authoring experiment — MAP1 through MAP2.1

The owner is designing a future finite island **as 2D geography first**, before any approved 3D translation. `MAP_CONCEPT/Scene_MapConcept.tscn` runs independently with **F6**, preserving the original numbered **Authored Preset** and the newer **Procedural Seed** mode.

**Progress as of November 2, 2026:**

- **BW-MAP1:** Numbered authored map preset with coastal features, mountains, forests, towns and candidate POIs.
- **BW-MAP2.0 / MAP2.0B:** Full 64-bit seeded landmasses, branching peninsulas, deep ocean inlets and small islands, independently illustrated and exported. The owner confirmed MAP2.0B results looked OK so far; more visual polishing may follow.
- **BW-MAP2.1 (owner-viewed in F6):** Inland mountain ridges, numbered **Mountains 1...N**, bounded count/coverage/ruggedness/length controls, lowland/foothill preview **129×89 normalized elevation**, illustrated contours. Ridges have their own independent seed stream; their sliders do not reshape the coast.
- **BW-MAP2.1B (implemented in source; awaiting C# build/F6):** A separate **4096×4096 u16 master elevation** at proposed **12×12 km** (2.93 m/sample), deterministic high-frequency surface detail, one cached shaded-relief image, improved 32× viewer zoom, panel-aware camera framing, independent mountain-local detail seed/bounds metadata and **JSON schema 6 + separate 32 MiB R16 binary export**. The original coastline and ridge placement algorithms are preserved. No local mountain re-roll tools yet.
- **MAP2.2 planned:** Inland lakes, freshwater rivers and drainage based on elevation, keeping saltwater inlets separate.
- **MAP2.3 planned:** Forest/meadow/wetland and biome zone placement.
- **MAP2.4 planned:** Variable seeded towns/cities, roads and POIs, validated for flat land and connectivity.
- **MAP2.5 planned:** Quality validation of many seeds, controlled variation, debug/export polish.
- **MAP3.0 planned:** Approve map coordinate scale/sea level/height interpretation and build a separate translator to actual WORLD procedural terrain **only after an explicit integration decision**.

**Scale note:** The authoring master is square and 4096 pixels wide, but the current island outline is still authored in the original ~1600×1080 conceptual drawing area inside that master. Adopt no fixed 12-km island *diameter* or metric mountain height from this alone. The binary map is a proposal for later import, not connected to live 3D terrain.

**Separation rule:** MAP_CONCEPT currently does **not** change the `WORLD/` marching-cubes terrain, 3D chunk meshes, loading/LOD, camera or gameplay. The existing BW-W4 streaming measurement and performance gates remain authoritative and independent. Before map-to-world integration, validate map masks, slope/sea levels, road clearances, world-space determinism and chunk-local queries; do not assume an illustrated region polygon is a physical 3D site. Full owned roadmap: [MAP_CONCEPT/IMPLEMENTATION_PLAN.md](../MAP_CONCEPT/IMPLEMENTATION_PLAN.md), and feature guide: [MAP_CONCEPT/README.md](../MAP_CONCEPT/README.md).

## 1. Target and priorities

Develop a performant, visually convincing procedural landscape in Godot 4 C# **without Terrain3D or Voxel Tools**. Longer-term ambitions are broad natural regions, rolling meadows, connected mountains, forested valleys, coastlines and eventually caves, overhangs, basements and enterable buildings.

**Performance is the top design constraint.** A 60 FPS target implies about 16.7 ms of total frame time including gameplay, rendering and physics; it is a budgeting target, **not** a demonstrated achievement. Watch frame-time spikes, peak memory, shader/physics setup and streaming latency—not just triangle count.

Engineering rules:

- Deterministic world-space generation, reproducible seeds, stable chunk coordinates and matching same-LOD boundaries.
- A reusable `WORLD/World.tscn` independent of `PLAYER/`; use `SCENE/Scene_World.tscn` for testing.
- No runtime allocation of a complete 32 km world; chunks should be resident only when needed.
- CPU data generation may run on bounded workers; scene tree, mesh resources and physics installation remain on the appropriate Godot main thread.
- Terrain mesh LOD, residency/streaming, physics residency, vegetation batching and simulation distance are **separate systems**.
- No unnecessary per-frame callbacks on dormant chunks.
- Preserve existing visual and performance behavior unless a measured improvement justifies a change.

## 2. What is already implemented (verified vs. unverified)

| Component | Location | Status / limitations |
| --- | --- | --- |
| Independent free-fly test scene | `SCENE/Scene_World.tscn`, `WORLD/Debug/` | **Runs locally**, according to screenshots; WASD/arrows, Q/E vertical, Shift speed, inverted mouse look |
| Reusable world scene | `WORLD/World.tscn` | **Implemented** as the scene instance used by Scene_World |
| World-space seeded terrain shaping | `WORLD/Generation/DensityGenerator.cs` | **BW-W3A revised after owner screenshot**: wider-scope rolling meadow hills, lower-frequency ridges and world-space coverage mask for separated mountain groups. Height-derived only, no caves; **new version awaits validation** |
| Classic indexed marching cubes | `WORLD/Generation/MarchingCubesMesher.cs` | **Previously visually validated** on nine low chunks; now extends vertical sampling to configurable bounds, using revised vertical edge-cache stride. Tall mountain topology **awaits validation** |
| Grass/rock color material | `WORLD/Materials/WorldTerrain.gdshader` | **Visually visible**, but deliberately basic; not the final terrain art |
| Static terrain and asynchronous CPU generation | `WORLD/WorldPreview.cs` | **Owner demonstrated 729/729 chunks** in the prior version; updated code supports 1–100 **explicit** chunks per side including even numbers, bounded six-result queue, single worker, cancellable regeneration and 12-chunk-per-frame cleanup |
| Optional physics in nearby chunks | `WORLD/WorldPreview.cs`, `WORLD/Configuration/WorldMaster.tres` | **Implemented**, default one centre-chunk collider; **walking/collision accuracy not yet validated** |
| Inspector settings | `WORLD/Configuration/WorldSettings.cs`, `WorldMaster.tres` | **Implemented**: earlier settings plus new MountainCoverage, valley/foothill/mountain controls, F1 temporary snapshot overrides (saved resource unchanged) |
| Generation statistics | `WORLD/WorldPreview.cs`, `WORLD/Debug/WorldTestScene.cs` | **Earlier HUD owner-tested**; F1 now includes direct NxN count, seed, seven sliders, detail and regeneration. The new UI awaits Godot validation |
| Test-scene scale landmark | `WORLD/Debug/WorldScaleTree.tscn`, `WorldScaleTree.cs`, `SCENE/Scene_World.tscn` | **Authored; awaiting local validation**. One 14 m pine under `WorldActorsRoot/DebugPropsRoot`, positioned using the height function; test-only, no collider and removable as one scene instance |
| Continuous terrain streaming / unload | — | **Not implemented**; all requested chunks are generated once, remain in memory |
| Distance-dependent mesh LOD and stitching | — | **Not implemented**; `MeshStep` is one uniform global resolution for the entire test patch |
| 3D cave/overhang editing, save state, forest, water | — | **Not implemented** |

**Current configurable static flyover (729 chunks owner-tested; revised generation awaits validation):** `ChunkRadius=13` produces **27×27 = 729 chunks**; `CellsPerChunk=32`, `CellSize=2 m` yields **64×64 m chunks** and a **1,728×1,728 m horizontal patch**. F1 now accepts **integer side count 1–100**, including evens (e.g. 9→81, 10→100 chunks), plus seed, seven terrain sliders and uniform MeshStep=1/2. Configurable vertical coverage is now **y=-16 to 300 m** independent of the 64 m horizontal chunk size; heights outside this range can still be clipped. Default `MeshStep=1` samples every **2 m** and steps 2/4 sample every **4/8 m**. `CollisionChunkRadius=0` means the centre chunk only, **not proximity-based streaming**. The bounded worker generates chunks centre-outward; static chunks are all retained after loading. Restore `ChunkRadius=1` and `CellSize=1 m` for historical 96 m measurements.

## 3. Observed performance — preserve these benchmarks

These numbers are **historical user-run screenshots from the original 96×96 m nine-chunk preview**, not independent automated benchmarks or results for the new 576×576 m configuration. The two runs used nominally identical generation settings. The screenshots are from slightly different viewpoints, so near/far visual equivalence is not rigorously established.

| Observed measurement | MeshStep 1 | MeshStep 2 | Interpretation |
| --- | ---: | ---: | --- |
| Horizontal patch | 96×96 m | 96×96 m | Same physical world size |
| Chunks loaded | 9 / 9 | 9 / 9 | Fixed preview, not streaming |
| Colliders | 1 | 1 | Centre chunk only |
| Vertices | 11,294 | 3,023 | ~73.2% fewer |
| Triangles | 21,308 | 5,394 | **~74.7% fewer** |
| Total CPU meshing | 19.8 ms | 7.8 ms | **~60.6% lower** in these runs |
| Worst individual CPU chunk | 4.9 ms | 3.7 ms | CPU-side, not render frame time |
| Total main-thread mesh setup | 14.4 ms | 12.8 ms | Only ~11.1% lower; fixed setup costs may dominate |
| Worst individual mesh setup | 9.1 ms | 8.8 ms | May approach/exceed a useful per-frame operation budget |
| Total collision cooking/setup | 6.9 ms | 4.5 ms | One collider in each test |
| Worst individual collider setup | 6.9 ms | 4.5 ms | Does **not** include future dynamic collider management |
| Worst observed frame delta | 127.0 ms | 116.7 ms | **Unexplained startup/initialization spikes** |
| Observed frames ≥33.3 ms | 2 | 2 | Sampling only while preview generation is active |

Prior to the first performance instrumentation pass, the original nine-chunk screenshot showed **21,308 triangles**, **74.5 ms combined CPU meshing** and **129.4 ms combined main-thread mesh/collision uploads**. These are a **different implementation/instrumentation state** (including different collision coverage), so do **not** claim apples-to-apples improvement versus the new measurements.

### Conclusions we can act on

1. **MeshStep 2 is promising for farther terrain.** From the submitted wide views its overall hills/silhouette appear similar to step 1, despite ~75% fewer triangles. Close-up edges, slopes, shadows, collision and seams still require comparison.
2. **CPU meshing improved strongly from step 1 → step 2 in these runs.** Do not extrapolate the ratio to terrain caves, arbitrary surfaces, massive regions or final frame rate.
3. **Mesh setup time did not fall nearly as much as triangle count.** Investigate fixed costs (mesh resource creation, array marshaling, node setup, shader/driver warm-up) rather than blindly reducing triangles.
4. **The two >100 ms worst-frame values remain unsolved.** A large startup frame delta is *not proven* to be caused by any one terrain operation. Identify first-run/editor initialization, shader compilation, scene loading, C# JIT/GC and physics setup effects separately.
5. **These timings are totals and worst operation times, not sustained FPS, GPU timing, or end-to-end streaming latency.** Do not report 60 FPS or zero stutters as established.

## 4. Implementation roadmap — prioritized Codex tickets

These tickets specify **recommended next tasks**, not permission to implement all of them in one change. Keep one authorized ticket per reviewable commit, update this plan with measured outcomes, and retain the independent `Scene_World` test.

### BW-W1.0b — Large static flyover checkpoint (EARLIER BASELINE; P0)

**Goal:** Use the existing 81-chunk **576×576 m** screenshot as the earlier baseline; current experiments now target up to 729 chunks without implementing streaming or mixed LOD.

- Preset is now `ChunkRadius=4`, `CellsPerChunk=32`, `CellSize=2`, `MeshStep=1`, centre-only collision. `SCENE/Scene_World.tscn` uses a raised, farther-reaching camera and faster free-fly movement.
- Worker generates concentric/square centre-out rings, so nearby chunks appear first. The scene still retains all 81 chunks and has **no dynamic unload/reload**.
- The debug HUD shows area, loaded chunks, timing and frame-spike counts; startup spikes print the previous frame operation (mesh/collider/idle). This is **correlation, not causation**. It also observes 30 extra frames after loading, then sleeps.
- **Acceptance:** In Godot 4 .NET, reach 81/81 chunks; fly across the middle, boundaries and far edges without holes or sustained FPS collapse. Record generation/setup/collision totals, peak frame delta, spike correlations, memory and editor/standalone conditions. Restore old settings for historical benchmark comparisons.
- **Status: 576 m terrain visually inspected by owner; final startup/FPS benchmarks remain incomplete.**

### BW-W2.2 — Earlier F1 size presets and safe regeneration (OWNER-TESTED STATIC GRID)

**User request:** The 14 m tree exposes the small perceived scale of the original mountain composition. Increase land **area ninefold** by preserving 64 m chunks and generating a 27×27 grid (1,728×1,728 m); make F1 reveal size/detail presets and a Regenerate button.

- `WORLD/Configuration/WorldMaster.tres` now defaults to **ChunkRadius=13** and `MeshStep=1` (2 m surface sampling). Available F1 presets are **576 m/81 chunks, 1,088 m/289 chunks, and 1,728 m/729 chunks**; optional uniform `MeshStep=2` is 4 m sampling.
- `WorldTestScene.cs` manages an F1 panel under `DebugUIRoot`; it sets the cursor visible for UI and restores mouse capture when closed, and does not modify the `.tres` resource.
- `WorldPreview.cs` cancels the current generation, uses per-generation output queues to discard stale results, serializes workers through a semaphore, throttles to six pending CPU chunks, and batch-removes existing chunks (12 per frame). One mesh or collider is still installed per frame; old results and pending collider requests do not survive regeneration.
- The large world is **not streamed**. All 729 chunks remain in memory until a regen or scene exit. Warn about memory, build time, collisions (centre only) and initial loading. Avoid promising 60 FPS.
- **Owner acceptance:** pull/build C#, run `SCENE/Scene_World.tscn` F6, press F1, choose each size/detail and Regenerate, confirm the HUD reaches the appropriate total with no duplicated old chunks, no runtime error and healthy frame times after construction. In particular, test switching large → small → large while a build is in progress. Verify the tree stays in `WorldActorsRoot/DebugPropsRoot` across rebuilds.
- **Status:** The prior 729-chunk static grid was owner-tested in a screenshot. This fixed-preset UI has since been superseded by the numeric count and shaping sliders documented below. Future work should prioritize real streaming rather than indefinitely expanding the static grid.

### BW-W2.3 / BW-W3A.1 — Numeric world size + terrain-shaping F1 panel (CURRENT; AWAITING VALIDATION)

**Motivation:** The owner's 27×27 screenshot has **729/729 chunks**, **2,157,117 vertices** and **4,136,496 triangles**. CPU meshing totaled **2,727.4 ms**, main-thread mesh setup **566.6 ms**, collision **6.2 ms**, max frame delta **110.4 ms**, and only two frames ≥33.3 ms during loading. Visually, the green lowland is bland and the far region becomes a uniform wall of tiny high peaks. These are owner-read screen numbers from the *earlier* generator, not post-change benchmarks.

**Implemented in source this pass:**

- F1 has a validated LineEdit text field for **chunks per side** (1–100, including even values), a seed box, detail selection, and **seven sliders**: Meadow variation, Valley half-width, Rolling hill height, Mountain mass, Peak height, Peak sharpness and Mountain coverage. Clicking Regenerate applies a single immutable runtime `TerrainParameters` snapshot; editing doesn't mutate the resource or start generation until clicked. Reset restores saved `WorldMaster.tres` defaults to the UI.
- WorldPreview's integer grid coordinates are chosen by explicit NxN count: `minimum = -count/2`, `maximum = minimum+count-1`, with centre-first rings. `ChunkCount = count*count`, and HUD world width = `count*ChunkWidth`. For even counts, the grid is one chunk farther toward negative axes; seams remain same-LOD.
- The original F1 regeneration safeguards (cancellation, six pending mesh outputs, single serialized CPU worker, 12 old chunk removals per frame and one mesh/collider install per frame) are preserved. The debug tree resnaps once to the revised seed/heights when regenerating.
- Geography now has stronger broad lowland variation, scattered meadow-hill relief, **low-frequency mountainous masses**, a world-space `MountainCoverage` mask forming separate groups/lower passes and less frequent, wider ridges. Parameters remain world-coordinate based to suit future streaming. The goal is to avoid a flat carpet and spiked wall, *not* to match the reference screenshot with textures/vegetation/lighting.

**Owner acceptance:** C# builds cleanly; F1 opens and shows seven value-labeled sliders; enter `9` to generate exactly `9×9=81`, `10` to generate `10×10=100`; changing seed, peaks, coverage and valley width produces noticeable results after Regenerate, while the user can still switch F1 and fly. Test quick regenerations before generation finishes, verify no duplicate chunks or stale results and no tree placement error, and note triangle counts/frame performance at 27×27. No live sliders until Regenerate is pressed.

**Performance note:** The 729-chunk static mode still holds millions of triangles in memory and has no dynamic residency. The next scalability task remains real streaming/LOD rather than another static grid expansion.

### BW-W2.4 — Optional 50×50 static stress test (SOURCE IMPLEMENTED; AWAITING LOCAL TEST)

The previous 27×27 editor cap was an **author-imposed precaution**, not a Godot or marching-cubes limit. The F1 input, UI validation, regeneration range and `WorldSettings.ChunkRadius` snapshot range now permit **up to 100×100**, with an explicit user confirmation beyond 50×50 (2,500 chunks); the startup default remains 27×27 (729 chunks). At 64 m per chunk, that spans **3,200×3,200 m (10.24 km²)**, around **3.43 times** the earlier 27×27 area.

The F1 panel displays the selected chunk count and world width, plus a high-load warning at 30×30 and above. Fly camera far clip is 4,300 m. Existing safeguards remain: one serialized worker, at most six queued CPU meshes, one main-thread setup operation per frame and batched old-chunk cleanup. All finished meshes stay resident; **there is still no streaming, distance LOD, or distant collision**. The user can choose uniform 4 m sampling to reduce geometry.

**Owner acceptance:** Enter `50`, regenerate, confirm HUD targets `2,500` chunks and `3,200 m` across, inspect camera traversal and frame time/memory, then switch back to 9 or 27 to test unloading. **Do not promise 60 FPS** for this heavy static stress case.

### BW-W2.5 — Typed chunk-count regression and optional 100×100 (FIX AUTHORED; AWAITING GODOT VALIDATION)

**Reported issue:** After the previous 50×50 limit increase, the owner typed 50 in F1 and clicked Regenerate, but the world still regenerated as 27×27. **Root cause is not confirmed**; the prior `SpinBox` kept typed text and its committed numeric `.Value` separately, potentially yielding a stale 27 at button click.

**Code changes:** Both chunk-count and seed fields are now Godot `LineEdit` nodes; `WorldTestScene.cs` parses `.Text.Trim()` at the **button click**, validates explicit integer ranges and does not silently clamp/fall back to the old value. The F1 label shows the parsed live count/size, and a `[WORLD F1] User requested ...` log line confirms the exact request sent to `WorldPreview.RegenerateWorld`. The world manager accepts 1–100 chunks per side and the terrain settings snapshot permits the required radius. A `ConfirmationDialog` must be accepted for requests **greater than 50×50**; 27×27 remains the safe saved startup default.

**Local validation:** Pull/rebuild Godot C#. Type 50 without pressing Enter, click Regenerate, confirm HUD goes to **2,500/2,500** and 3,200 m, not 729. Test 9→81, 10→100, and invalid text should be rejected. Test 100→confirmation→10,000 chunks only if willing to risk high memory/stutters. At 64 m chunks, 100×100 is **6,400×6,400 m (40.96 km²)** and is a severe **static renderer/memory stress test**, not streaming. Record Godot Output and build errors if the 50 regression persists.

**Do not infer:** supporting 100 as an input does not establish that 10,000 meshes will run smoothly on the owner's GTX 1080 or that the world has dynamic chunk streaming.

### BW-W1.1 — Explain startup frame spikes (FOLLOW-UP; P0)

**Goal:** Determine why the preview's max observed frame delta remains 116–127 ms even when CPU meshing and mesh setup totals are much smaller.

**Suggested work (small, focused):**

- Capture a repeatable **cold-run vs warm-run** profile with the same seed/resolution/viewpoint; report build configuration, resolution, GPU/CPU, Godot editor vs standalone and whether the NVIDIA overlay was active.
- Measure frame-time and setup milestones using Godot profiler or appropriately scoped timestamps: scene initialization, first frame, first/each `ArrayMesh` upload, collider creation and any shader/resource warm-up.
- Separate the time before/after first terrain results from the chunk installation; current `_Process(delta)` sampling is active only during startup, so it may count a startup pause that began before the first chunk.
- Optionally compare **`GenerateCollision=false`** with centre-only collision, and `MeshStep=1` vs `2` **without changing other settings**.
- Keep per-frame diagnostic overhead small; if logging is excessive, aggregate and print at the end. Avoid expensive profiling code in production builds.

**Acceptance:** Provide a small before/after table with **max/95th-or-99th frame time**, warm-run results and a reasoned suspected/confirmed source for each >33.3 ms spike. If inconclusive, state that rather than inventing a fix. No gameplay or terrain-shape changes.

### BW-W1.2 — Optimize main-thread setup and physics residency (P0/P1)

**Goal:** Reduce frame spikes and avoid cooking physics for invisible/distant areas.

- Profile mesh buffer upload vs node creation vs collider creation and measure allocation/GC if practical.
- Compare batching/reusing nodes or cached objects only if profiling supports doing so. Never replace an optimized indexed mesh with per-triangle nodes.
- Make per-frame main-thread work **budget-aware**, not merely “one operation per frame” (one expensive collider operation can still exceed budget).
- Keep collision local to the player/test probe; install/uninstall collision safely as regions become relevant. Static preview's `CollisionChunkRadius` is only a demonstration flag.
- For near-surface traversal, verify physical interaction/clearance and proper generated shape winding.

**Acceptance:** Same visual surface, same triangle and seed results at unchanged `MeshStep`, no new bad frame spikes, measured setup/collider cost and a documented collider policy.

### BW-W2.1 — Correctness and deterministic adjacent chunks (P1)

**Goal:** Ensure the geometry can support hundreds of chunks before streaming starts.

- Write focused tests for seed determinism, negative chunk coordinates, positions and collision consistency.
- Check both sides of shared edges: no geometry gaps, normal discontinuities or winding changes in **same-resolution** chunks; use world-space density and shared normals.
- Inspect topology on slopes and at the 0/32 m vertical volume boundaries; avoid clipped terrain when heights exceed the prototype's vertical extent.
- Preserve physics fallback and limits. Verify `MeshStep=2` at **near-player** view and steep slopes; don't approve it solely from distant screenshots.

**Acceptance:** Reproducible generation for the same seed regardless of chunk construction order, robust same-LOD seams and a manual close-up comparison at steps 1 and 2.

### BW-W4 programme — Prove streaming under disposable content before gameplay imports

**Owner direction (November 1, 2026):** Ben Final is a new finite-island survival project. Before moving selected War Machine systems into it, prove reliable, low-hitch terrain generation/residency and progressively test **temporary** vegetation, props and simple moving entities. All stress content must be opt-in and removable without deleting the reusable streaming engine. This section supersedes the old suggestion to immediately port War Machine content or scale up static resident grids.

**System boundaries:** One world-level desired/resident chunk policy should decide *where content is relevant*. Separate owners manage **terrain mesh data**, **GPU mesh nodes**, **near terrain collision**, **foliage MultiMeshes**, **static props** and **active actor simulation**, each with independent budgets and safe teardown. Terrain generation must remain world-seeded and order-independent. The finite island can have large logical bounds without allocating or generating the entire area.

**Prior architecture evidence:** `ben0kay/2d-isometric` has `WORLD/Chunks/ChunkStreamer.cs` with nearby-first requested columns and load/unload hysteresis. War Machine's `implement-td0-td1` branch has `WORLD/Streaming/StaticSectionStreamer.cs` with bounded jobs, asset residency and cleanup, but **that implementation streams static prefabs/buildings, not terrain**. Adapt their policies; **do not copy foreign terrain plugins or pipelines**. Ben Final's `WorldPreview` already has a bounded CPU mesh queue, cancellation and single-operation-per-frame integration, but is currently **finite static build only**, not camera-following streaming.

**Critical visibility constraint:** The debug camera currently has a 4,300 m far clip, while nine 64 m terrain chunks per side cover only 576 m across. An initial 9×9 streamer would visibly end close to the camera if that entire far range is left exposed. Either deliberately shorten the effective visibility with fog/far plane during the first residency proof or supply a separate cheap distant-terrain representation later. **Do not solve this by keeping thousands of full-resolution chunks resident.**

### BW-W4.0 — Reproducible benchmark and streaming observability (SOURCE IMPLEMENTED; AWAITING GODOT TEST)

- Preserve `SCENE/Scene_World.tscn` static F1 mode for A/B comparison; add an explicitly optional streaming stress-test path (independent debug scene or F1 opt-in). Avoid changing playable project startup or island data.
- Instrument **generation CPU**, worker queue wait, main-thread `ArrayMesh` creation/upload, `MeshInstance3D` attachment, trimesh collision construction/removal, GPU/vegetation batch uploads (later), actor spawn/despawn (later) and garbage collection where measurable. Sample actual per-frame deltas *even when no work is active*. Track median, p95, p99, worst frame, stutters over 33.3 ms, chunk-request latency and warm vs cold traversal.
- Report **desired, queued, computing, ready, resident, collider-ready and unloading chunk counts**, resident triangle count, bounded result-buffer size, load/unload counts and memory estimates/available engine counters. Don't treat total mesh-build milliseconds or FPS alone as proof of no hitch.
- A repeatable camera route should cover ordinary speed, current 180 m/s Shift fly, rapid reversing, 2 km teleport, return to old positions, regeneration during loading, and scene stop/restart. Record Godot version, display resolution, graphics settings, warm/cold shaders and route, plus RAM/VRAM where accessible.
- **Acceptance:** The current static baseline can be reproduced, diagnostic counters tell *which stage* correlates with frame spikes, and no high-frequency logging itself adds stalls. No fabricated or assumed timings.

**Implemented BW-W4.0 source:** `WORLD/Debug/FrameTimeRecorder.cs` is a constant-space half-millisecond histogram recorded at every foreground `Scene_World` process frame; it computes average, median, P95, P99, worst and the configurable spike count without sorting or allocations per frame. `WORLD/Debug/WorldTestScene.cs` keeps a small always-on FPS/frame-delta/P99/render-primitives indicator (updated at 2 Hz) and F1 Start Manual / Start Route / Stop controls, with a summary of sampled managed heap and collection counts. The generator already records CPU meshing, main-thread mesh installation and collision cooking; `WorldPreview.cs` now additionally times batched removal. All work stages remain distinct, and logging occurs on capture start/finish, not each frame.

**Repeatable route:** `WorldFlyCamera.cs` can travel a fixed camera path across the currently selected world width at **60 or 180 m/s**, with an optional instantaneous reposition to a corner followed by a return trip. The route camera starts from a fixed world-space position/heading, 180 m above world zero; the initial view and flight path are reproducible for a given NxN size and seed. **World size affects teleport distance; 2 km is not guaranteed on the default 27×27 patch.** Starting a capture closes the F1 panel; stop or inspect it by reopening F1. The route automatically ends and prints one summary to Godot Output. Manual capture continues until stopped.

**Limitations, intentionally deferred:** this static preview does not have wanted/resident/requested chunk identities, streaming load latency, dynamically unloading chunks or distant-L0D/actor/foliage metrics. Those counters become valid with BW-W4.1 and later fixtures; don't invent them in W4.0. `delta` measures **frame-to-frame time** in a foreground process callback, not GPU frame time or independently measured mesh upload cost. Percentiles are quantized at 0.5 ms, with a 1,000 ms histogram overflow bucket; measured managed heap is **not total process RAM or VRAM**. Terrain-stage correlation is only suggestive, not proof of spike causation. The initial 27×27 patch and generator settings stay unchanged; no War Machine imports or disposable entities have been added.

**Owner F6 acceptance:** open `SCENE/Scene_World.tscn`, build .NET C#, press F1 and start Manual Capture, fly around and stop. Check the HUD p99/spike count and one Output summary. Run the deterministic route at 60 and 180 m/s, with and without instant reposition; verify return to manual control and F1 settings, compare results **on the same NxN/seed/detail and viewport size**. Test during cold generation and again after it settles. Observe no runaway GC or log spam. Record an actual screenshot and Godot errors, if any. **Source only reviewed; runtime/build measurements not performed.**

### BW-W4.1 — Real streaming and chunk residency (P1)

**Prerequisite:** BW-W4.0 benchmark/diagnostics and W2 seam/coordinate sanity checks. Treat unresolved W1 >100 ms startup anomalies as part of the baseline rather than ignoring them.

- Replace static full-grid initialization with player/camera-centered requests keyed by stable integer chunk coordinates.
- Separate chunk **data generation**, **GPU mesh residency**, **collision residency** and **world object/foliage residency**.
- Use bounded worker queue(s), request priorities, backpressure, cancellation/epoch tokens for stale work, unload and node/resource disposal.
- Update only when the camera crosses a threshold/region or a relevant setting changes; use hysteresis. Avoid full world scans and needless work every frame.
- Measure high-speed traversal and camera teleport; do not queue unlimited chunks or leave holes indefinitely.

**First implementation scope:** Same-resolution **64 m terrain chunks only**, finite logical bounds from the island/world configuration, and a small default resident envelope (suggest **9×9 around the fly camera** plus a deliberately bounded unload buffer). Keep the same world-seed and terrain settings; reject obsolete tasks using generation IDs, dedupe coordinates, and rank near/forward-visible requests ahead of less useful ones. Only compute a bounded amount of pending work; limit installation by **measured milliseconds as well as operation count**, because one large mesh/collider can stall despite a one-op-per-frame rule. Use load/unload hysteresis and a resident cap with explicit behaviour when all slots are occupied. When idle, stop scheduling work.

**Acceptance:** Camera crosses multiple boundaries and only a bounded number of chunks/resources/jobs remain resident, with no stale installs, duplicate meshes, accumulating abandoned buffers or missing current-area chunks. Full scene stop/return, teleport, regeneration and rapid direction reversals must be safe. Test collision readiness separately for eventual FPS movement. Record p99/worst frame deltas, latency and memory. **Status: Planned.**

### BW-W4.2 — Actual near/far LOD with valid transitions (P1/P2)

**Prerequisite:** Stable dynamic residency.

- Treat `MeshStep=1,2,4` as proven **uniform-resolution comparison knobs**, not as a working LOD system.
- Evaluate near step 1, medium step 2, distant step 4 **only after** defining camera-dependent LOD selection and scene budgets.
- Implement crack-free mixed-resolution transitions (tested stitching/transition cells, e.g. Transvoxel-inspired if appropriate, or another verified approach). Matching shared height samples alone is insufficient.
- Consider distant height-mesh/impostor representation for surface-only areas if it improves time/quality; explicitly evaluate the join to volumetric regions before adopting a hybrid.
- Limit expensive collision to near regions and avoid per-LOD collider churn.

**Acceptance:** No visible cracks between step 1/2/4 neighbours, LOD switching is stable while moving and temporal artifacts are acceptable; measured triangle, CPU/GPU time and memory at fixed view distances.

### BW-W4.3 — Disposable vegetation streaming stress fixtures (PLANNED; after terrain residency)

- Add **test-only generated** tree variants, grass/fern/bush clumps and region-density presets. These stand in for later authored War Machine foliage; do **not** import War Machine assets yet.
- Place deterministically from world seed and chunk/world coordinates. Test dense forest edges, clearings, grass beds, mixed slopes and empty regions. Preserve the ability to author future island vegetation zones without tying foliage to terrain mesh ownership.
- Use chunk/region `MultiMeshInstance3D` batches instead of one node per grass blade/tree. Compare shadows on/off, sparse vs heavy densities, near/far ranges and alpha overdraw. Do not give distant grass physics.
- Instrument generation, bulk instance uploads, memory, render primitives/draw calls and frame-time spikes. Enforce bounded build/disposal work; unloading terrain must not orphan vegetation.
- **Acceptance:** Walk/fly past the same forest repeatedly and check that all temporary foliage reappears identically, with stable batch counts, no permanent memory growth and no unexplained streaming stalls. **Dispose of test models after performance acceptance, not the reusable placement contracts.**

### BW-W4.4 — Disposable props and moving-entity stress fixtures (PLANNED)

- Add separate opt-in test-only crate/rock/placeholder-building placements and a configurable group of simple moving dummy actors. No combat AI, inventory, world-save contract or transferred War Machine scenes.
- Use stable deterministic placement/actor IDs; define which content is persistent in world description versus temporarily active in scene tree. A chunk unloaded and reloaded must never duplicate props or actors.
- Budget spawn/despawn, near collision setup and simulated entity ticks separately from terrain and foliage. Far actors sleep or despawn by policy; do not continue updating 500 distant entities simply because their meshes are hidden.
- Stress-test fast movement, boundary oscillation, teleport, density changes, return trips, entering occupied chunks and scene reload. Report per-stage timings and counts.
- **Acceptance:** Temporary actors and props consistently appear/disappear with no duplicate instances, broken physics, unbounded updates, cross-frame crash or runaway memory. The generic residency hooks survive fixture deletion. **Status: Planned.**

### BW-W4.5 — Streaming stability gate and cleanup (PLANNED)

- Repeat a documented **five-minute** traversal route on the target test PC, for **terrain-only**, **terrain+vegetation** and **full disposable load**. Record **cold** first traversal and **warm** return separately; allow first-use shader compilation to be identified, not hidden.
- **Initial engineering targets, not guarantees:** sustained gameplay target ~60 FPS (16.7 ms per frame); aim for **p99 ≤25 ms** and no repeatable **streaming-induced frame above 33.3 ms** after warm-up during ordinary traversal. Teleport, first-use shader compilation and editor overhead should be reported separately. If hardware misses the target, profile and tune rather than mark accepted.
- Verify that resident/pending object counts plateau, no old jobs reattach after teleport/regeneration, no crashes after backtracking, and CPU/GPU/RAM use stays within an observed reasonable budget. Startup time and peak memory are also part of acceptance.
- Remove or disable all placeholder trees, grass, props and entities from the actual game scene **after** measurements while preserving generic stream coordination, test harness, data contracts and diagnostics. Keep a repeatable disposable benchmark scene or generator available for future regression tests.
- **War Machine porting is gated:** no weapons, zombies, buildings or final art enter Ben Final until this gate is reviewed with the owner. Re-test incrementally after each real system is introduced.

### BW-W3A — Composed geography (IMPLEMENTED IN SOURCE; AWAITING OWNER VALIDATION)

The reference layout is **foreground meadows / winding lowland valley → side foothills → connected jagged range toward negative Z**. `WORLD/Generation/DensityGenerator.cs` now combines regional masks, low-frequency terrain, smoother foothills, broad mountain mass and narrow ridged peaks. It uses seed-deterministic world-space coordinates without per-chunk boundary resets.

- `ValleyHalfWidth` widens/narrows flatter traversable ground. `RidgeAmplitude` (legacy export name) tunes rolling hills. `MountainAmplitude`, `PeakAmplitude`, `PeakSharpness` and northward `MountainStartZ/FullZ` control the distant skyline.
- Default heights in the current prototype seed, from a **formula-level numerical sample** (not a Godot render or FPS benchmark), are roughly 12–26 m in the foreground and can reach ~160 m in the far range.
- The previous assumption that vertical sample height == horizontal chunk width clipped tall geography. The mesher now uses independent `TerrainBottomY=-16` / `TerrainTopY=300` metre limits and checks only vertical cells around the height-derived surface. Its shared-edge vertex cache now uses a **vertical stride** sized for the configured elevation range, avoiding key collisions when Y indices exceed horizontal N.
- Existing 9×9 static world, worker queue, debug HUD, slope coloring, standalone scale tree and category root structure are preserved; a small high-altitude material tint is added. The camera starts near z=240 and faces north for a better composition view.
- **Acceptance:** owner opens `Scene_World` F6 and sees flatter usable land at the foreground, progressively rolling foothills, distinct high jagged peaks on the far side and the 14 m landmark. Confirm terrain doesn't stop at y=64, adjacent chunk seams remain sealed, collision at the central patch works, and generation/frame time remains acceptable. Record screenshot plus 81/81 chunk count.
- **Limitations:** mountain surfaces are still height-derived, so vertical cliffs, caverns, undercuts and realistic erosional drainage are not yet solved. Do not confuse the simulated height map with true 3D density.

### BW-W3B — True 3D caves and overhangs (PLANNED)

- Design at least one repeatable terrain scenario: grassy lowland → rolling foothills → connected ridged mountain range.
- Expose large-scale shape/warp/height bands separately from local detail; avoid noisy terrain everywhere.
- For caves, overhangs and excavation, migrate from `Height(x,z)-y` shortcut to **full 3D signed-density sampling** and appropriate vertically stacked chunk indexing. Re-evaluate normal generation, bounds and ambiguous marching-cubes cases.
- Plan building/basement excavation as persisted volume edits separate from generated seed terrain; loading a building mesh must not be assumed to carve ground.
- Test geometry, physics, precision and blending before large-scale generation.

**Acceptance:** Coherent seed-deterministic landscape at eye level, controlled overhang/cave opening, stable collision and unchanged bounded-memory principles. Do not try to build the final 32 km world in this phase.

### BW-W5 — Biomes, vegetation and visual depth (P2)

- After geometry/streaming is stable, add material rules for rock/grass/snow by slope and height, sky/fog and planned coast/water.
- Implement deterministic tree/grass placement by biome/terrain queries, sparse on steep cliffs and under dense shade.
- Use region-level instancing/LODs, shadow limits and culling rather than thousands of individually processed grass nodes.
- Use source models under `ASSETS/`, with explicit approval for imports from previous projects.

**Acceptance:** Eye-level screenshot of grassland/forest/rock regions at a documented FPS/quality setting with stable streaming, appropriate memory and no obvious tiling.

### BW-P1 — Persistent modifications and integration (later)

- Design seed/version configuration separately from saved terrain edits and world objects.
- Integrate `PLAYER/Player.tscn` into a **separate composition scene**; do not couple its input/movement code to `WORLD/`.
- Evaluate floating origin/chunk-relative coordinates for very large distances once meaningful traversal requires it.

## 5. Architecture guardrails for future agents

- **World data vs Godot resources:** `DensityGenerator` and `MarchingCubesMesher` run off-thread only with pure managed immutable data. `WorldPreview` owns Godot resources, scene integration and physics, on the correct thread.
- **Coordinate convention:** world X/Z are horizontal and Y is up. Positive density = solid, negative = air, zero = surface; preserve consistent triangle winding and normals (see root `LESSONS_LEARNED.md` for the black-terrain incident).
- **Resolution invariants:** `ChunkWidth = CellsPerChunk * CellSize`. `MeshStep` must divide nominal cell count exactly; stepping changes sampling resolution, **not world-space chunk width**.
- **Memory:** do not keep density caches, CPU arrays, colliders or meshes indefinitely without explicit residency. Benchmark memory allocation, not just polygon count.
- **Vertical extent:** the current y=0..32 m bounds are a preview constraint. Plan robust world-height ranges, caves and full volume ownership before production code.
- **Quality:** lower resolution can remove close-up details. Re-test steep slopes, surface normals, physics and chunk joins after each meshing change.
- **Profiling:** report worst/p95/p99 frame time, CPU generation, mesh setup, collision cooking, active triangles/nodes, peak memory and sample scenario. Note cold vs warm results and editor vs export.
- **Scope:** no auto-install of terrain addons, no unapproved mass rewriting, no speculative streaming/LOD claims, and no unbounded worker queues.
- **Git:** preserve all existing `PLAYER/` and `SCENE/` content, honor CodePreferences, commit one requested ticket at a time.

## 6. Codex start-of-task checklist

1. Read [../AGENTS.md](../AGENTS.md), [../CodePreferences.md](../CodePreferences.md), [../README.md](../README.md), [WORLD_AUTHORING.md](WORLD_AUTHORING.md), this plan, and [WORLD README](README.md).
2. Inspect current `WORLD/WorldPreview.cs`, `Generation/MarchingCubesMesher.cs`, `Generation/DensityGenerator.cs`, `Configuration/WorldSettings.cs` / `WorldMaster.tres`, and the world test scene; do **not** assume this snapshot is still current.
3. Confirm with the owner which **one ticket** is authorized. The **current first choice is BW-W4.0 instrumentation/baseline**, incorporating the old BW-W1.1 startup-spike investigation before starting BW-W4.1. Do not bundle the later foliage/actor stress fixtures into one giant pass.
4. Keep `Scene_World` independently runnable, preserve visible terrain and follow the short-check policy. State **tests performed vs unverified**, and don't fabricate benchmark numbers.
5. Update this plan's status/findings and the root README's milestone status when verified scope changes. Add a root lessons entry only for a significant reusable confirmed mistake.

**Latest conclusion (priority revised November 1, 2026):** **No War Machine gameplay imports yet.** First instrument and prove camera-following terrain residency, then distance representation and **disposable** vegetation/prop/actor stress testing, then remove benchmark objects. Keep `MeshStep=1` as the near-terrain baseline and `MeshStep=2` as a candidate for further tests, not working LOD. The existing >100 ms startup frame deltas, main-thread setup spikes and future LOD joins remain risks to diagnose before a seamlessness claim.
