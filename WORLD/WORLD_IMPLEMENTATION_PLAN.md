# WORLD Implementation Plan — Ben Final

> **Purpose:** Working technical handoff for Codex and future AI agents. Read before changing `WORLD/` code. This is a **plan and test record**, not authorization to implement every stage.
>
> **Repository:** `ben0kay/ben-final` — current development branch `main`.
> **Last documented user tests:** October 30, 2026 (Godot 4.7.2 .NET, screenshots supplied in chat).
> **State:** The owner ran **27×27 = 729 chunks (1,728 m)** in Godot and found the lowlands bland and the mountains a continuous wall of thin spikes. The F1 panel now accepts **1–100 chunks per side**, any integer, and exposes seven terrain sliders plus seed and mesh detail; the default terrain now has more rolling relief and broad mountain coverage masks. **The new pass awaits local validation.** Streaming, mixed LOD, caves, vegetation and persistence remain unimplemented.
>
> **Companion documents:** [WORLD_AUTHORING.md](WORLD_AUTHORING.md) explains the intended geology/terrain techniques; [README.md](README.md) explains existing files and how to run them; [root README](../README.md) tracks the project's milestone statuses; [CodePreferences](../CodePreferences.md) and [AGENTS](../AGENTS.md) govern coding and Git practice.

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

### BW-W4.1 — Real streaming and chunk residency (P1)

**Prerequisite:** W1 spike diagnosis, W2 seam/coordinate correctness.

- Replace static full-grid initialization with player/camera-centered requests keyed by stable integer chunk coordinates.
- Separate chunk **data generation**, **GPU mesh residency**, **collision residency** and **world object/foliage residency**.
- Use bounded worker queue(s), request priorities, backpressure, cancellation/epoch tokens for stale work, unload and node/resource disposal.
- Update only when the camera crosses a threshold/region or a relevant setting changes; use hysteresis. Avoid full world scans and needless work every frame.
- Measure high-speed traversal and camera teleport; do not queue unlimited chunks or leave holes indefinitely.

**Acceptance:** Camera crosses multiple chunk boundaries and only a bounded number of chunks/resources/jobs remain resident; no crash or stale chunk installs; record frame spikes and memory.

### BW-W4.2 — Actual near/far LOD with valid transitions (P1/P2)

**Prerequisite:** Stable dynamic residency.

- Treat `MeshStep=1,2,4` as proven **uniform-resolution comparison knobs**, not as a working LOD system.
- Evaluate near step 1, medium step 2, distant step 4 **only after** defining camera-dependent LOD selection and scene budgets.
- Implement crack-free mixed-resolution transitions (tested stitching/transition cells, e.g. Transvoxel-inspired if appropriate, or another verified approach). Matching shared height samples alone is insufficient.
- Consider distant height-mesh/impostor representation for surface-only areas if it improves time/quality; explicitly evaluate the join to volumetric regions before adopting a hybrid.
- Limit expensive collision to near regions and avoid per-LOD collider churn.

**Acceptance:** No visible cracks between step 1/2/4 neighbours, LOD switching is stable while moving and temporal artifacts are acceptable; measured triangle, CPU/GPU time and memory at fixed view distances.

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
3. Confirm with the owner which **one ticket** is authorized. By default, **propose BW-W1.1 startup-spike diagnosis before implementation of streaming or distance LOD**.
4. Keep `Scene_World` independently runnable, preserve visible terrain and follow the short-check policy. State **tests performed vs unverified**, and don't fabricate benchmark numbers.
5. Update this plan's status/findings and the root README's milestone status when verified scope changes. Add a root lessons entry only for a significant reusable confirmed mistake.

**Latest conclusion:** Keep `MeshStep=1` as a near-terrain quality baseline and treat `MeshStep=2` as a promising candidate for medium/distant terrain, **not an adopted production LOD**. The unresolved >100 ms startup frame deltas and future mixed-resolution seams are the next major risks to investigate.
