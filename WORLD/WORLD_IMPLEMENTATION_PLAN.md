# WORLD Implementation Plan — Ben Final

> **Purpose:** Working technical handoff for Codex and future AI agents. Read before changing `WORLD/` code. This is a **plan and test record**, not authorization to implement every stage.
>
> **Repository:** `ben0kay/ben-final` — current development branch `main`.
> **Last documented user tests:** October 30, 2026 (Godot 4.7.2 .NET, screenshots supplied in chat).
> **State:** Original 96×96 m visual marching-cubes preview and mesh-resolution benchmarks were run on the owner's PC. An expanded **576×576 m static flyover** (81 chunks) is now authored but **has not been run/benchmarked by the owner yet**. Dynamic streaming, distance LOD, caves, vegetation, persistence and a production-scale world are **not implemented**.
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
| World-space seeded terrain shaping | `WORLD/Generation/DensityGenerator.cs` | **Implemented**; currently **height-derived density** `Height(x,z)-y`, not volumetric caves |
| Classic indexed marching cubes | `WORLD/Generation/MarchingCubesMesher.cs` | **Implemented and visually working** on nine equal-resolution chunks; indexed shared-edge vertices and reuse of precomputed heights for normals |
| Grass/rock color material | `WORLD/Materials/WorldTerrain.gdshader` | **Visually visible**, but deliberately basic; not the final terrain art |
| Fixed terrain patch and asynchronous CPU generation | `WORLD/WorldPreview.cs` | **Implemented**, one managed worker calculates chunk mesh data centre-first; one mesh or collider operation per main-thread frame. **Expanded 9×9 preset awaits validation** |
| Optional physics in nearby chunks | `WORLD/WorldPreview.cs`, `WORLD/Configuration/WorldMaster.tres` | **Implemented**, default one centre-chunk collider; **walking/collision accuracy not yet validated** |
| Inspector settings | `WORLD/Configuration/WorldSettings.cs`, `WorldMaster.tres` | **Implemented**: seed, geography amplitudes, chunk size, mesh step, collider radius, debug thresholds |
| Generation statistics | `WORLD/WorldPreview.cs`, `WORLD/Debug/WorldTestScene.cs` | **Runs locally** in earlier small test: mesh/physics timing and spike count. Expanded test adds preceding-frame operation correlation and 30 post-load diagnostic frames; **not yet verified** |
| Test-scene scale landmark | `WORLD/Debug/WorldScaleTree.tscn`, `WorldScaleTree.cs`, `SCENE/Scene_World.tscn` | **Authored; awaiting local validation**. One 14 m pine under `WorldActorsRoot/DebugPropsRoot`, positioned using the height function; test-only, no collider and removable as one scene instance |
| Continuous terrain streaming / unload | — | **Not implemented**; all requested chunks are generated once, remain in memory |
| Distance-dependent mesh LOD and stitching | — | **Not implemented**; `MeshStep` is one uniform global resolution for the entire test patch |
| 3D cave/overhang editing, save state, forest, water | — | **Not implemented** |

**Current configured larger flyover (awaiting validation):** `ChunkRadius=4` produces **9×9 = 81 chunks**; `CellsPerChunk=32`, `CellSize=2 m` yields **64×64 m chunks** and a **576×576 m horizontal patch**. Nominal vertical coverage is **y=0 to 64 m**; heights outside the volume can be clipped. Default `MeshStep=1` samples every **2 m** and steps 2/4 sample every **4/8 m**. `CollisionChunkRadius=0` means the centre chunk only, **not proximity-based streaming**. The worker generates chunks centre-outward; they are all retained after loading. Restore `ChunkRadius=1` and `CellSize=1 m` for historical 96 m measurements.

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

### BW-W1.0b — Large static flyover checkpoint (CURRENT TEST; P0)

**Goal:** Exercise 81 visible chunks over **576×576 m** without prematurely introducing streaming or mixed-resolution LOD.

- Preset is now `ChunkRadius=4`, `CellsPerChunk=32`, `CellSize=2`, `MeshStep=1`, centre-only collision. `SCENE/Scene_World.tscn` uses a raised, farther-reaching camera and faster free-fly movement.
- Worker generates concentric/square centre-out rings, so nearby chunks appear first. The scene still retains all 81 chunks and has **no dynamic unload/reload**.
- The debug HUD shows area, loaded chunks, timing and frame-spike counts; startup spikes print the previous frame operation (mesh/collider/idle). This is **correlation, not causation**. It also observes 30 extra frames after loading, then sleeps.
- **Acceptance:** In Godot 4 .NET, reach 81/81 chunks; fly across the middle, boundaries and far edges without holes or sustained FPS collapse. Record generation/setup/collision totals, peak frame delta, spike correlations, memory and editor/standalone conditions. Restore old settings for historical benchmark comparisons.
- **Status: Authored, awaiting owner run and FPS validation.**

### BW-W1.1 — Explain startup frame spikes (NEXT DIAGNOSTIC; P0)

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

### BW-W3 — Coherent geography and true 3D density (P2; can prototype in parallel)

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
