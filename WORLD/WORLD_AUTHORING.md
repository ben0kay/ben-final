# WORLD Authoring Guide — Ben Final

> **Status: AUTHORING STANDARD.** Custom marching-cubes terrain and a large static preview exist. The owner tested 27×27 chunks and requested stronger macro geography; a numeric 1–50 NxN F1 world-size control (50×50 is a heavy static stress test) and seven composition sliders are now authored but awaiting local validation. Caves, streaming, water and distance LOD remain unimplemented.
>
> **Scope:** `WORLD/` and the existing independent `SCENE/Scene_World.tscn`.  
> **Engine:** Godot 4 .NET / C#. **Terrain3D, Voxel Tools and other terrain plugins are not assumed or required.**  
> **Read first:** [root README](../README.md), [AGENTS.md](../AGENTS.md), [CodePreferences.md](../CodePreferences.md), [WORLD README](README.md).

## 1. Vision — the landscape we want

The visual reference is an expansive natural landscape that feels **kilometres wide** rather than a small noise-covered game map:

- **Foreground:** Lush meadow grass with visible individual blades, density changes, small ground patches and gentle rolling relief.
- **Middle distance:** Broad forested valleys and irregular, non-uniform tree lines. Open grassland should transition believably into dense tree cover.
- **Far distance:** Strong mountain silhouettes with some sharp ridges, connected mountain ranges, cliffs and optional high-altitude snow.
- **Large geographical forms:** Valleys, hills, plateaus, mountain foothills, coastlines, lakes and natural drainage basins; not randomly distributed miniature mountains.
- **Atmosphere:** Sky, sun direction, shadowing, distance haze/fog, colour depth and atmospheric perspective make the scale readable.
- **Water:** Where chosen, large bodies of water have coherent shorelines, reflection/lighting and separate water rendering. Marching cubes does not implement water.
- **Exploration:** Eventually support first-person traversal, enterable buildings, caves, overhangs and optional basement terrain excavation.

**Quality target:** Believable, attractive terrain at first-person eye level, with good performance and consistent visual style. The screenshots are an aspiration, not a requirement to reproduce them exactly or evidence that a given algorithm alone creates the visuals. Practical art direction may be slightly stylized to suit performance.

**Design principle:** Good procedural terrain is a **geography pipeline** (large forms → local forms → material/biome rules → vegetation placement → lighting/atmosphere), not simply stacking high-frequency noise or running marching cubes.

## 2. Terrain representation — density + marching cubes

### Density field

Use a deterministic function `Density(worldX, worldY, worldZ, seed, configuration)`. Start with the explicit convention:

- **positive density = solid**
- **negative density = empty air**
- **zero = surface**

For an uncomplicated hillside: `density = terrainHeight(worldX, worldZ) - worldY`.

Later add three-dimensional carving for caves or overhangs by combining 3D density functions. A tunnel, for example, can subtract a cave volume via an appropriate signed-distance operation. The density source must be independent of a camera, test scene, chunk creation order or current thread.

### Marching cubes

1. Sample density at the eight corners of a cube.
2. Classify the corners as inside/outside; derive the 8-bit case.
3. Interpolate crossings along edges and emit the appropriate triangles from a validated lookup table.
4. Generate consistent winding, smooth normals, and mesh surfaces.
5. Repeat for all cells in each active chunk; build terrain collision from an appropriate mesh or simplified nearby collider.

**Critical:** A chunk with `N` cells per axis needs **`N+1` samples per axis** to cover both edges. Adjacent chunks sample identical world coordinates along shared boundaries. For 32 cells at 1 m resolution, that is 33×33×33 samples, not a world sized voxel allocation.

Remember that marching cubes generates polygons **where the density crosses zero**; it does not automatically generate appealing mountain shapes, textures, vegetation, caves, water, good LOD or streaming.

### Optional hybrid strategy

If benchmarks favour it, consider inexpensive surface mesh/heightfield geometry for ordinary distant terrain while using true volumetric density chunks near caves, excavations or overhangs. **Do not silently change algorithms or add a terrain plugin**; assess compatibility, junctions, physics and persistence first.

## 3. Designing coherent geography

Keep distinct configurable scales, then blend them deliberately. Seed all noise generators; sample using world coordinates so independently loaded chunks agree.

| Scale / stage | Candidate technique | Intended visible result |
| --- | --- | --- |
| Continental / regional | Very low-frequency elevation masks and region fields | Areas of lowlands, highlands and broad water basins |
| Mountains | Ridge/ridged noise plus mountain-range masks | Connected peaks, steep slopes and sharp silhouettes rather than random isolated spikes |
| Valleys / foothills | Smoothly blended masks, broad erosion-inspired shaping | Gradual transitions from meadow to forest to mountain |
| Local relief | Multi-octave noise with restrained amplitude | Natural slope variation without noisy ground |
| Warping | Low-amplitude domain warping | Less straight or overly regular terrain features |
| Drainage / optional erosion | River masks or erosion pass, benchmarked separately | More believable valley lines, gullies and water routes |
| 3D volumes | Density carve/union operations | Overhangs, natural tunnels, caves and rock openings |

Do not start by running computationally expensive hydraulic erosion across a huge world. It's an optional later stage: tile boundaries, determinism, memory and offline/precompute strategy must be established before adopting it.

### Slope and mountain transitions

Use regional masks to control mountain influence. Ensure elevation and steepness change over broad distances; don't place abrupt high peaks directly beside featureless meadow unless that landform is intentional. Expose parameters for maximum elevation, ridge strength, lowland smoothness, foothill width and roughness.

### Caves and basements

Use controlled 3D density fields for caverns, tunnels and cave entrances. Keep caves from unintentionally perforating the terrain surface except at planned openings. A building can eventually request an excavation volume, but that requires a **separate integration contract** and persistence of edits; simply spawning a building mesh does not carve terrain. Test cave ceilings, collision, surface normals, entrances and interaction with streamed chunks.

## 4. Biomes and surface materials

Compute biome inputs deterministically and separately from mesh generation: world position, elevation, slope, aspect, moisture, temperature proxy, distance to water, exposure and any regional biome masks.

Suggested initial biome categories (all **planned**):

- **Meadow / lowland:** varied grass height and coverage, sparse shrubs, occasional rocks.
- **Temperate or conifer forest:** clustered trees with organic gaps, undergrowth and reduced grass beneath dense canopy.
- **Mountain foothills:** thinner vegetation, more rock and increasing slope.
- **Rock / cliff:** rock-dominant material on steep gradients, little or no grass.
- **Alpine / snow:** snow based on altitude plus exposure/slope; avoid a perfectly flat horizontal snowline.
- **Lakeshore / coast:** terrain-to-water transition with slope/material constraints; no underwater grass.

Materials should transition gradually with slope/height/biome weights rather than sharp bands. Prefer shader/material reuse and a small configurable palette; evaluate triplanar texturing where it reduces stretched cliff texture UVs. Mesh normals and material blending should not reveal chunk boundaries.

## 5. Vegetation — lush without overwhelming the renderer

Vegetation is a distinct procedural system driven by terrain/biome queries, not part of marching cubes.

- **Trees:** use stable seeded region placement with spacing rules, ecological clusters and clearings; vary species, scale, rotation and density. Avoid straight grid rows.
- **Grass:** dense in suitable open areas; thinner on steep rock, snow, beaches, water, under deep shade and around heavily trafficked clearings. Avoid a perfect uniform carpet.
- **Shrubs / rocks / litter:** additional layers when justified; use slope, humidity, forest density and altitude masks.
- **Near ↔ far:** separate grass visibility, tree model LODs, shadow ranges, culled instances and (later) forest impostors where useful.
- **Rendering:** use `MultiMeshInstance3D` or equivalent batching per manageable region/material. Avoid thousands of per-blade `Node3D` instances or full physics on every grass tuft.
- **Collision:** only assign meaningful colliders to relevant trunks/large rocks, with simple shapes and local residency.
- **Reuse:** reference models and vegetation authoring lessons from `final-machine` and `war-machine`, but import/copy only with explicit approval; use `ASSETS/` for imported source models and textures.

Vegetation positions must be stable across unload/reload and adjacent chunk borders. Use world-aligned region placement, stable seed hashes and boundary-safe placement; avoid visible patterns when crossing a chunk seam.

## 6. Water, sky and atmosphere

Water should be a separate renderer/system (plane or coastline-aware surface as appropriate), not water voxels by default. Sea level, lake levels and coastal biome masks must agree with terrain generation.

Aim for readable light direction, shadow detail close to the player, distant atmospheric haze, sun reflection on water, and believable distance falloff. Use controlled fog and lighting before attempting expensive visual effects. Consider high elevations and ridgelines when selecting far clipping and atmospheric settings.

## 7. World chunks, distance LOD and streaming

**Starting experiment:** ~512×512 m active test area; initial candidate surface chunk width **32 m**. A 3D volume chunk might be 32×32×32 m with 1 m sampling for early experiments. These are tunable **proposals**, not fixed production specifications.

**Long-term ambition:** potentially **32 km × 32 km** (1,024 km²) with procedural generation. That does not mean storing all terrain or voxels in memory at once, and does not guarantee acceptable performance.

- Represent chunk addresses with stable integer coordinates and correct floor handling for negatives.
- Use a **quadtree candidate** for horizontal surface LOD / region selection; use stacked 3D chunks or another validated volumetric index for caves. A quadtree by itself is not a volumetric terrain structure.
- Distinguish **LOD** (mesh resolution) from **streaming** (whether chunk data/nodes reside in memory).
- For seams at the **same resolution**, sample exact world-space boundary positions and use compatible normals.
- For **different resolutions**, implement and validate stitching, transition cells (e.g. a Transvoxel-style technique), skirts where appropriate or another tested solution. Matching just the shared sample positions does not solve all LOD cracks.
- Use concentric residency/LOD budgets and avoid rapidly regenerating chunks when the player hovers near a threshold (hysteresis).
- Keep collision/detail close to player; distant geometry can use cheaper meshes and no expensive per-triangle collision.
- Water, foliage, structures, AI and persistence each have independent residency and LOD choices.

### Threading design

Pure calculation (noise/density buffers, triangle arrays, candidate foliage placements) may run on a **bounded worker queue** using C# threads/tasks. Avoid unsafe Godot scene-tree calls on workers. Integrate/render/attach resource and node changes on the proper thread in controlled time budgets; check actual Godot 4 API thread-safety guarantees before implementing.

Track chunk states (requested, generating, ready, active, unloading) and reject late/stale results when the requested LOD or origin has changed. Use cancellation/generation tokens and upper bounds on outstanding requests. Fast movement must not cause unlimited allocations or lag spikes.

### Large-world coordinates

A 32 km world can expose single-precision position problems. Evaluate local/chunk-relative coordinates, floating origin or an appropriate double-precision build when scaling requires it. Keep persistent absolute positions and chunk identity independent of the shifted local rendering origin.

## 8. Data ownership and persistence

- Separate immutable procedural seed/version/configuration from mutable player edits, constructed structures, harvested objects and persistent entities.
- Save terrain edits as sparse chunk-local differences or explicit edit operations with versioning; do not overwrite modifications when a chunk regenerates.
- Keep world-generation rules independent of the FPS scene; Scene_World must work with a camera/debug mover alone.
- Expose focused world queries/interfaces: density sample, surface/raycast, biome/material weights, suitable spawn location, terrain modification request, world seed/coordinate conversions.
- Do not hard-code player node paths, debug-camera paths, particular room names or other scene-dependent references inside density generation.

## 9. Proposed code ownership (create incrementally)

~~~text
WORLD/
├── README.md                   [exists]
├── WORLD_AUTHORING.md          [exists]
├── World.tscn                  [planned reusable world scene]
├── Generation/                 [planned]
│   ├── DensityGenerator.cs
│   ├── MarchingCubes.cs
│   ├── GeographyGenerator.cs
│   └── BiomeGenerator.cs
├── Streaming/                  [planned]
│   ├── ChunkManager.cs
│   ├── ChunkJobQueue.cs
│   └── TerrainLOD.cs
├── Vegetation/                 [planned]
├── Materials/                  [planned]
├── Water/                      [planned]
└── Configuration/              [planned]
~~~

**Implementation update:** `WORLD/World.tscn`, `WorldPreview.cs`, `Generation/DensityGenerator.cs`, `Generation/MarchingCubesMesher.cs`, `Configuration/WorldSettings.cs` + `WorldMaster.tres`, `Materials/WorldTerrain.gdshader`, and debug scripts now exist as a **bounded static landscape prototype**. The remainder of the architecture above (biomes, streaming, vegetation, etc.) stays planned. Consult [WORLD/README.md](README.md) for current controls and validation status.

## 10. Milestones and acceptance checks

The root [README](../README.md) is authoritative for milestone statuses. For current **owner-run performance observations** and prioritized Codex tickets, always consult [WORLD_IMPLEMENTATION_PLAN.md](WORLD_IMPLEMENTATION_PLAN.md). Suggested world checkpoints:

1. **BW-W1 — Surface proof (visually demonstrated in owner screenshots):** The original 3×3 patch used classic marching cubes with a height-derived density, indexed meshes and grass/rock shading. A larger 9×9 static patch (576×576 m) is now configured; inspect its winding, collision and longer-distance continuity locally.
2. **BW-W2 — Same-LOD neighbouring chunks (awaiting validation):** neighbour chunks are generated by world-space density sampling; verify visually that shared boundaries have no gaps/shading seams and that seeds are reproducible.
3. **BW-W3A — Composed geography (owner feedback: too flat / dense spikes):** numeric F1 geo sliders, broader rolling hill relief and broken-up mountain region coverage were revised. Verify mixed lowlands and distant distinct peaks, lack of cliffs clipping, same-LOD joins and performance before accepting.
4. **BW-W3B — Volumetric caves (planned):** extend density to controlled entrances, overhangs and excavation; verify no unintended holes.
5. **BW-W4 — Stream/LOD:** move rapidly across chunk boundaries without persistent holes, stale chunks or unbounded queue growth; inspect mismatched LOD seams and frame-time spikes.
6. **BW-W5 — Lush world pass:** forest density gradients, rocks/grass, altitude/slope exclusions, distant mountains, fog/lighting and measured near/far rendering.
7. **BW-P1 / integration:** persistent edits, spawn placement, large-world precision and larger traversal scenarios only after the small world is stable.

### Debug outputs and manual checks

Provide development toggles (when implemented) for chunk bounds, current LOD, active/queued chunk counts, seed, position, collision state, biome weights and generation time. Review screenshots at player eye level, hilltops, forests and shorelines—not only from an aerial camera.

Performance measurements must state hardware, render resolution, vegetation count/density, scene, position and settings. **Do not claim 60 FPS, kilometre-scale capacity or successful cave support without actual checks.** The owner typically performs Godot visual/performance validation; agents keep tests brief unless more is explicitly requested.

### BW-W1 performance diagnostic pass (awaiting local validation)

The finite `SCENE/Scene_World.tscn` preview now exposes uniform `MeshStep` settings 1, 2 and 4 while preserving each chunk's physical area. It also caches neighbouring column heights for surface-normal gradients (with outside samples at chunk boundaries), decouples `ArrayMesh` setup from expensive trimesh collision into separate frames, and defaults collision to the central chunk only. See [WORLD/README.md](README.md) for measurement procedure and original screenshot baseline.

Every generated chunk reports CPU meshing and mesh setup time; each generated collider reports physics cooking time. This is **not GPU-completion timing** or a continuous in-game frame profiler. Coarser `MeshStep` is only a preview quality/performance experiment; it is **not streamed LOD** and does not solve mixed-resolution transition cracks. Do not claim the result is visually lossless or faster until tested in the owner's Godot runtime.

## 11. Things not to do

- Do not introduce Terrain3D, Voxel Tools or other world plugins without authorization.
- Do not make endless high-frequency noise everywhere and call it realistic terrain.
- Do not use a purely 2D quadtree as if it represented all underground 3D space.
- Do not allocate a 32 km³ volumetric grid or generate whole-world terrain meshes at startup.
- Do not place vegetation uniformly, spawn expensive individual grass nodes or provide distant full collision by default.
- Do not let worker threads mutate active Godot scene trees unsafely.
- Do not alter existing assets or automatically transfer vegetation/engine code from other repos.
- Do not claim a generated world looks like the reference until it is inspected in Godot.

**Operating rule:** Author the geography intentionally, measure complexity and cost, keep systems independent, and improve visual quality in repeatable passes.
