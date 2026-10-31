# WORLD — Procedural World Preview

**Status:** BW-W1 / BW-W2 first-pass implementation authored. **Awaiting local Godot C# build, visual inspection and collision/seam validation.** This is a finite preview, **not** terrain streaming, LOD or a cave system.

**Primary guide:** [WORLD_AUTHORING.md](WORLD_AUTHORING.md). Follow it for terrain, biome, cave, streaming, chunk LOD, vegetation, water and longer-term geography work.

## Working preview files

```text
WORLD/
├── World.tscn                         Reusable terrain preview scene
├── WorldPreview.cs                     Generation queue, main-thread mesh/collider installation, statistics
├── Configuration/
│   ├── WorldSettings.cs                Inspector parameters (seed, size, resolution, amplitude)
│   └── WorldMaster.tres                Default settings
├── Generation/
│   ├── DensityGenerator.cs             Deterministic, seamless managed height-derived density
│   ├── MarchingCubesMesher.cs          Indexed 256-case classic marching cubes
│   └── LOOKUP_TABLE_LICENSE.md        Triangulation-table attribution
├── Materials/
│   └── WorldTerrain.gdshader          Lightweight grass/rock slope blend
└── Debug/
    ├── WorldFlyCamera.cs               Independent, keyboard/mouse free camera
    └── WorldTestScene.cs               HUD statistics adapter
```

The test environment is [SCENE/Scene_World.tscn](../SCENE/Scene_World.tscn). It instances `WORLD/World.tscn`; **PLAYER is not involved**.

## Run it

1. Pull the latest Ben Final commit, then build C# in Godot 4.7.2 .NET.
2. Open `SCENE/Scene_World.tscn` and press **F6** (Run Current Scene). This doesn't replace the F5 main scene.
3. Look around with the **mouse** (vertical inverted by default). **WASD or arrow keys** fly; **Q/E** descend/ascend; hold **Shift** for faster flight. **Escape** releases the mouse and left-click recaptures.
4. Watch the top-left live counters: attached chunks, total vertices/triangles, cumulative CPU generation milliseconds, cumulative main-thread mesh/collision upload milliseconds.
5. Select `WORLD/Configuration/WorldMaster.tres` in the Inspector to change seed, terrain height/roughness, number of chunks and resolution; run the scene again to regenerate.

### Initial settings

- 3×3 chunks (`ChunkRadius = 1`); 32 metres wide per chunk (`CellsPerChunk = 32`, `CellSize = 1`), a **96×96 m** patch.
- Each chunk is a 32³ bounded volume; height-derived density is sampled per x/z column and only surface-crossing vertical cells are meshed.
- Positive density means solid; negative means air. Surface normals derive from world-space gradients.
- One managed worker **sequentially** generates CPU-only chunk data. The Godot main thread attaches at most **one finished chunk per frame**, with optional trimesh collision (default on).
- Every chunk becomes dormant after it is attached; the world polling callback also disables itself after all nine chunks are installed.
- No terrain plugin, player dependency, foliage, caves, persistence, dynamic streaming, scene-wide LOD or terrain edits.

### Performance caveats

Generation timings are **not frame times or FPS measurements**. A chunk's ArrayMesh upload and collision creation can still cause a frame spike; future phases should measure that separately before scaling up. The current implementation uses classic marching-cubes cases, which are not ambiguity-safe for complex cave topology; that is a later engineering stage.

The custom terrain material is deliberately minimal and **does not represent the final mountain/forest look**. It's an algorithm test with a simple grassy/rocky hillside.

## Roadmap

1. **BW-W1:** after the initial black-terrain report, the triangle winding and sky ambient source were corrected. Inspect that geometry is green/rocky, shading is correctly oriented, and collision behaves. **Local validation pending.**
2. **BW-W2:** inspect same-LOD chunk seams and normal continuity across borders.
3. **BW-W3:** richer geography, rock cliffs and volumetric cave features (after verifying 3D density).
4. **BW-W4:** true chunk residency, worker budgets, LOD and crack-free transitions.
5. **BW-W5:** materials, grass/trees, world biomes, water and atmospheric distance rendering.

For code conventions and independent world/test-scene ownership, read [root README](../README.md), [AGENTS](../AGENTS.md), and [CodePreferences](../CodePreferences.md).
