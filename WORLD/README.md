# WORLD — Procedural World Preview

**Status:** The 1,728×1,728 m, 27×27-chunk static preview was tested in Godot and loaded 729/729 chunks. The screenshot revealed a mostly bland plain and an almost continuous overly spiky mountain wall. The next pass adds an **F1 integer chunks-per-side field (1–27)**, seed entry and seven geography sliders; terrain noise is adjusted to create separated mountain groups and more rolling land. **New code awaits Godot validation.** Not streaming, LOD or caves.

**Continuation plan:** [WORLD_IMPLEMENTATION_PLAN.md](WORLD_IMPLEMENTATION_PLAN.md) — owner-run benchmarks, findings, P0/P1/P2 work tickets and acceptance tests for Codex. **Authoring guide:** [WORLD_AUTHORING.md](WORLD_AUTHORING.md). Keep generation, rendering, collisions, future vegetation and LOD under WORLD, separate from PLAYER.

## Source ownership

```text
WORLD/
├── World.tscn                         Reusable terrain preview scene
├── WorldPreview.cs                     Bounded worker queue, regeneration, deferred deletion, mesh/collision timings
├── Configuration/
│   ├── WorldSettings.cs                Shared settings: mesh step, collision range, performance warnings
│   └── WorldMaster.tres                Inspector-editable default values
├── Generation/
│   ├── DensityGenerator.cs             Seeded regional valley/foothills/mountain range height-derived density
│   ├── MarchingCubesMesher.cs          Indexed cubes; independently bounded vertical sampling for tall peaks
│   └── LOOKUP_TABLE_LICENSE.md        Original table attribution
├── Materials/
│   └── WorldTerrain.gdshader          Grassy/rocky slope blend
└── Debug/
    ├── WorldFlyCamera.cs               Free camera for test environment
    └── WorldTestScene.cs               HUD; F1 chunk-count field, seed, terrain sliders, regen
```

[SCENE/Scene_World.tscn](../SCENE/Scene_World.tscn) instances WORLD/World.tscn and **does not require PLAYER**.

### World size reference tree

`SCENE/Scene_World.tscn` now contains **one test-only, ~14 m pine-shaped reference tree** directly ahead of the starting fly-camera view. The reusable prop is `WORLD/Debug/WorldScaleTree.tscn` with a tiny one-shot ground placement script (`WorldScaleTree.cs`). It queries the actual world height function at its x/z coordinates, sinks the trunk base 0.3 m, has **no collision**, no idle update and no dependencies on imported foliage assets. The tree helps judge the size of hills and the currently selected world extent; it is **not** procedural forest spawning, nor a claim of final-quality vegetation. The instance now lives under **`Scene_World/WorldActorsRoot/DebugPropsRoot/ScalePine_14m`**, separate from generated terrain. To unplug it, **hide or delete that single node in `SCENE/Scene_World.tscn`**; the generator and `WORLD/World.tscn` will still function. The standalone model/script live in [`WORLD/Debug/WorldScaleTree.tscn`](Debug/WorldScaleTree.tscn) and [`WORLD/Debug/WorldScaleTree.cs`](Debug/WorldScaleTree.cs); delete these source files only if no other scene uses them. Its explicit `WorldNodePath = "../../../World"` resolves the reusable world after the category reparent.

## Run and inspect

1. Pull Ben Final, build C# in Godot 4.7.2 .NET, open `SCENE/Scene_World.tscn`, press **F6**.
2. Press **F1** to show/hide the **World Generator** panel. The cursor is freed for editing. Enter **chunks per side**, e.g. `9` = **9 × 9 = 81** chunks; `10` = **10 × 10 = 100** chunks. Set the seed, adjust geography sliders and uniform mesh detail, then click **Regenerate world**.
3. The scene removes prior terrain in frame-budgeted batches, cancels its old worker job, then generates the new terrain centre-outward. The stats HUD shows current extent, installed chunks, triangles, CPU/mesh/collision timings and limited spike diagnostics.
4. Fly with WASD/arrow keys, Q/E height and Shift faster; Escape releases the mouse. Default free-fly speed is **60 m/s**, or **180 m/s with Shift**.
5. For permanent startup defaults, edit `WORLD/Configuration/WorldMaster.tres` before F6. The F1 panel changes **runtime-only settings** and does not overwrite the saved resource. **Reset sliders to saved defaults** restores values in the panel but does not regenerate until you click Regenerate.

### F1 world-size and geography controls

**World size:** `Chunks per side` is a numeric SpinBox that accepts **any integer from 1 through 27**. Each chunk is currently 64×64 metres at the default `CellsPerChunk=32, CellSize=2`. Examples:

| Enter | Generated chunks | Physical world width |
| ---: | ---: | ---: |
| 9 | **9×9 = 81** | 576 m |
| 10 | **10×10 = 100** | 640 m |
| 17 | **17×17 = 289** | 1,088 m |
| 27 | **27×27 = 729** | 1,728 m |

Even values are supported: for `10×10`, the stable integer coordinates run -5 through +4 on each horizontal axis; for `9×9` they run -4 through +4. The displayed total and number of generated chunks match the actual input.

**Uniform mesh detail:** choose `MeshStep = 1` (2 m surface sampling) or `MeshStep = 2` (4 m). This is an entire-world quality comparison, not streamed distance-based LOD.

**Generator controls (all applied only by Regenerate):**

| F1 control | New saved default | Effect |
| --- | ---: | --- |
| World seed | 147 | Reproducible geography |
| Meadow variation | 8 m | Broad lowland height noise, higher = less uniform green plain |
| Valley half-width | 90 m | Width of the meandering flatter corridor |
| Rolling hill height | 30 m | Foothill and scattered meadow-hill relief |
| Mountain mass | 95 m | Broad mountain uplift |
| Peak height | 80 m | Additional sharp crest elevation |
| Peak sharpness | 2.75 | Larger = narrower/spikier crests; lower = broader peaks |
| Mountain coverage | 65% | Broad world-space mask controlling how much of the northern/flank zone becomes mountain groups |

**Performance and regeneration:** one worker runs at a time, a **six-result** bounded CPU mesh queue limits producer backlog, existing chunks are removed in batches of **12 per frame**, and at most one mesh or collider is installed per frame. The old generation is cancelled and stale results discarded when you press Regenerate again. The scale tree is resnapped to the new seed/heights; it can still be removed entirely from `WorldActorsRoot/DebugPropsRoot`. After loading, all chunks stay resident—this remains a **static world**, so 27×27 is much more resource-intensive than 9×9.

**Known limitation:** the North mountain layout is defined in *world metres* rather than relative fractions of each selected map width. Changing chunk count reveals more or less of one **consistent larger procedural geography**, not a resized copy of a fixed layout. This is deliberate for eventual streaming. The new mountain coverage mask reduces the old wall, and peak noise is less frequent, but visual suitability is **not yet verified**.

### Owner's large static-world screenshot (before geography-slider update)

The 27×27 test reached **729/729 chunks** with **2,157,117 vertices** and **4,136,496 triangles**; the HUD reported **2,727.4 ms total CPU meshing**, **566.6 ms mesh setup**, **6.2 ms centre-collider cooking**, **110.4 ms worst observed frame delta** and **2 frames above 33.3 ms**. These are startup/loading measurements, **not sustained FPS**. The owner found the large plains visually bland and the peaks too uniformly spiky. The updated defaults and coverage logic address that design issue; **do not claim performance improvement without a new Godot run**.

### BW-W3A terrain layout — new reference composition

The test camera starts near **(8, 63, 240)** looking toward **negative Z / north**. The 14 m `ScalePine_14m` remains near **(8, ground height, 90)**. Current seed is 147.

- **Foreground (positive Z):** a broad meadow/valley corridor with gentle elevation noise and isolated mid-sized hill masses; still suitable for future paths and structures.
- **Middle ground (roughly Z 80 to -100):** rolling side foothills that rise smoothly outside the meandering valley, not random peaks spread over every metre.
- **Far ground (negative Z, especially beyond about -100):** several broad world-space mountain groups separated by clearer lower passes; lower-frequency ridge detail replaces the previous uniform spiky wall. Mask and peak settings are deterministic across chunk boundaries.
- **Material:** existing green/rock blending is preserved; a subtle high-altitude light-stone/snow tint improves silhouette readability, without imported textures or vegetation.

**Key Inspector controls** in `WORLD/Configuration/WorldMaster.tres` (defaults are tentative, not visually approved):

| Group | Setting | Default | Purpose |
| --- | --- | ---: | --- |
| Lowlands | `BaseHeight` | 14 m | Basic meadow elevation |
| Lowlands | `BroadAmplitude` / `DetailAmplitude` | 8 / 1.5 m | Stronger broad waviness plus small-scale variation |
| Lowlands | `ValleyHalfWidth` | 90 m | Width of central flatter corridor |
| Foothills | `RidgeAmplitude` | 30 m | Rolling hills and separate meadow rises |
| Mountains | `MountainAmplitude` | 95 m | Large connected background uplift |
| Mountains | `PeakAmplitude` / `PeakSharpness` | 80 m / 2.75 | Wider ridges by default (sharpness remains adjustable) |
| Mountains | `MountainCoverage` | 0.65 | Mask for separated mountain groups / lower passes |
| Mountains | `MountainStartZ` / `MountainFullZ` | -40 / -220 m | Northward rise start and near-full range influence |
| Vertical range | `TerrainBottomY` / `TerrainTopY` | -16 / 300 m | Limits supported surface elevations, independent of 64 m chunk width |

The mesher still samples **only cells around the surface**; it does not allocate an `81 × 300m` volumetric density grid. Raising the vertical range increases supported height **without making the horizontal chunks any larger**. The surface remains a height-derived density (`Height(x,z)-y`): steep mountain walls are possible, **true caves/overhangs are not**. Same-resolution seams still need checking.

To test: build C#, F6 `SCENE/Scene_World.tscn`, let the selected chunk count finish (729 by default), fly from the meadow toward negative Z, and compare the ridge line from both aerial and low-altitude views. Check no flat-topped clipping, holes, major polygon spikes or collision errors; note the per-chunk CPU/mesh timing. Adjust F1 Meadow / Hills / Mountain Coverage / Peak Sharpness, one at a time, then click Regenerate to compare. **Do not infer FPS improvement from the original small-world measurements:** this is a different seed-based terrain function with greater relief.

### Recommended performance controls

| Property | Default | Meaning |
| --- | ---: | --- |
| `ChunkRadius` | 13 | Saved *startup* setting for 27×27. The F1 field directly accepts 1–27 chunks per side, including even counts |
| `CellsPerChunk` | 32 | Nominal cells along each chunk axis |
| `CellSize` | 2.0 m | New large-world preset: 32 × 2 m = **64 m per chunk**, 9 × 64 m = **576 m across**. Earlier screenshot baseline used 1.0 m |
| `MeshStep` | 1 | Current 2 m sampling; 2 = 4 m sampling; 4 = 8 m sampling. **Physical chunk width remains 64 m** |
| `GenerateCollision` | true | Global physics switch |
| `CollisionChunkRadius` | 0 | -1 = none; 0 = centre only; 1 = centre 3 × 3; larger values enable larger physics squares. Leave at 0 for free flight |
| `MainThreadWarningMs` | 8.0 ms | Logs individual mesh/collision setup work exceeding threshold |
| `FrameSpikeThresholdMs` | 33.3 ms | Counts frame deltas at or above threshold **during the preview build** |

If a requested `MeshStep` does not evenly divide `CellsPerChunk`, it falls back to step 1 rather than create a different-width chunk. Use 32 cells when comparing 1, 2 and 4. To restore the **original small 96×96 m** benchmark, set **ChunkRadius = 1, CellSize = 1.0, MeshStep = 1**, and keep the same seed. Other camera/scene settings now differ.

The new mesher **reuses its already-computed surface heights for interior normals** rather than recomputing four noisy height evaluations for every sample. Boundary points still sample outside their chunk so normals agree across same-resolution neighbours. The old indexed-vertex cache and geometric winding are preserved.

### What was optimized

- Terrain geometry is generated by **one serialized CPU worker** into a **bounded six-mesh queue** with no persistent streaming. Chunks are generated **centre-outward** for useful near-camera content first, and old chunks are freed gradually during F1 regeneration.
- **One main-thread operation per frame:** mesh setup and expensive trimesh collider creation are performed in separate frames, not together.
- Collision defaults to the **centre chunk only**, avoiding distant static-physics setup for the hundreds of surrounding demonstration chunks. Change the collision radius if you need to walk on surrounding chunks.
- The first few, last and any slow chunks print CPU/mesh timings and triangle count; slow collision work is also logged. The HUD summarizes totals/maxima without dumping 729 log lines.
- Each individual operation can log a slow warning. **Observed frame delta** is sampled while constructing the static preview and for **30 additional frames** after the final chunk, then the world disables its processing. Spikes are tagged according to the preceding frame's mesh/collider/other activity as correlation clues **not proven causes**.
- `MeshStep` gives a **fair same-area resolution comparison** rather than shrinking the world to reduce polygon count. Coarser steps may remove subtle ridges and are not claimed visually equivalent.

### Baseline and comparison procedure

The user’s **pre-optimization** screenshot (before this change) reported **9/9 chunks, 11,294 vertices, 21,308 triangles, 74.5 ms total CPU meshing and 129.4 ms combined main-thread mesh/collision work**. Those numbers don't distinguish setup from physics, and **cannot be used as the after measurement**.

The original results below were obtained from a **3×3, 96×96 m** patch, not the new large default. To reproduce comparable old-resolution numbers, first restore **ChunkRadius 1 / CellSize 1**. For a fair step 1 versus step 2 comparison, keep seed, camera, grid and physical chunk width constant:

1. Run `MeshStep = 1` with `CollisionChunkRadius = 0` and record triangle count, CPU generation, mesh setup and collision totals.
2. Change only `MeshStep = 2`, rerun, compare timing, triangle totals and screenshots at the same camera position.
3. Optionally test `MeshStep = 4`; check hill silhouettes and whether the ground looks too faceted.
4. To quantify the actual cost of collision, compare `CollisionChunkRadius = -1`, `0` and `1` **at the same mesh step**.
5. Check Godot Profiler/frame graph while flying or starting the scene. Printed total milliseconds are **not FPS**; engine/GPU work may be deferred beyond `ArrayMesh` setup time.

**Owner test update (original 96×96 m patch):** The latest two screenshot runs recorded **21,308 triangles / 19.8 ms CPU generation** at `MeshStep=1` versus **5,394 triangles / 7.8 ms** at `MeshStep=2` (same nine chunks; centre collider only). That's ~74.7% fewer triangles with broadly similar appearance in wide shots, but two frames still exceeded 33.3 ms in both runs and max frame deltas exceeded 100 ms. See [WORLD_IMPLEMENTATION_PLAN.md](WORLD_IMPLEMENTATION_PLAN.md) for full measured table, qualifications and next steps.

Do **not** infer sustained FPS or mixed-resolution LOD correctness from this single comparison. LOD stitching and dynamic near/far chunk selection belong to **BW-W4**, and the coarser setting is currently uniform across all chunks.

## Larger flyover experiment — configured, not yet measured in new scale

The default `WorldMaster.tres` now uses **ChunkRadius 13, CellsPerChunk 32, CellSize 2 m and MeshStep 1**, yielding **1,728×1,728 m** from **729 visible 64-metre chunks**. F1 also allows the prior 576-metre setting. The height-derived surface sampling now supports elevations between `TerrainBottomY = -16` m and `TerrainTopY = 300` m, independently from chunk width. It still cannot produce volumetric caves or overhangs. The fly camera moves at **60 m/s** (or **180 m/s with Shift**) and has a 2,600-metre far clip for the larger world.

**Test:** Launch `SCENE/Scene_World.tscn` with F6; the scene should fill the nearby area first and eventually attach **729/729** chunks by default. Let the build finish, then fly across the middle, towards each edge and back. Inspect screenshot continuity, close-up terrain shape, any chunk holes and the Godot profiler's frame graph. Note total triangle count, CPU meshing, mesh/collision setup, spike correlation counters, and whether spikes persist **after loading**. The scene doesn't unload chunks when you move; it is **not a streaming benchmark**.

To test cheaper geometry across any one selected patch, use F1 to choose `MeshStep=2` (4-metre sampling), then regenerate. It may visibly lose close-up detail; do not assume the previous 75% triangle reduction will yield the same timings. To avoid collisions slowing the flyover, keep `CollisionChunkRadius=0` (centre only) or set `GenerateCollision=false`.

## Next validation

Verify all **729** terrain chunks show the intended meadow/foothills/mountains without unexpected flat tops or missing cliff surfaces, preserve the earlier black-terrain winding/lighting fix, inspect same-resolution borders, confirm only the intended colliders exist, and check Godot **Output** for per-chunk timing warnings. Startup spikes may be acceptable for this static loading test; gameplay streaming stutter remains a future concern.

The next unimplemented systems are true streaming/residency, distance-dependent LOD transition geometry, caves and vegetation. See [root roadmap](../README.md).
