# Ben Final

**Repository:** ben0kay/ben-final  
**Engine:** Godot 4 .NET / C# (existing Ben Final project.godot preserved; C# support configured)  
**Status:** Initial FPS controller and independent marching-cubes WORLD preview authored; BW-W1 performance instrumentation and same-area mesh-resolution comparison added. Local Godot build and performance validation remain pending. Streaming, dynamic LOD and caves remain planned.

## Vision

Ben Final is a clean, modular foundation for experimenting with a first-person, simulation-focused game in a large, natural, procedurally generated world. The first technical priority is a **custom Godot terrain generator**—not an installed terrain plugin—exploring 3D density fields, marching cubes, chunk streaming, distance-based level of detail (LOD), and background generation. The long-term game may involve wilderness exploration, scavenging, enterable buildings, basements, caves, survival systems, and AI; those are directions, **not commitments or implemented features**.

The project deliberately separates **world technology** from **FPS gameplay** so either can be developed and tested without the other. Existing work from War Machine and Final Machine is a reference library; copy over individual proven components only when specifically approved and compatible.

## Core development approach: independent scenes

Build each subsystem in its own scene and test environment; bring them together later through **scene instancing and shared contracts**, not by merging unrelated source code or making one monolithic test scene.

| Development scene | Primary purpose | Dependencies |
| --- | --- | --- |
| **SCENE/Scene_Player.tscn** | Arrow-key FPS, inverted mouse look, RMB jump and right-Ctrl crouch | Instances reusable PLAYER/Player.tscn; graybox with collision (awaiting validation) |
| **SCENE/Scene_World.tscn** | Static 3×3 marching-cubes hills, generation timing and free-fly camera (first pass) | Instances WORLD/World.tscn; does not require Player; awaiting validation |
| **SCENE/Scene_Buildings.tscn** | Buildings, interiors, doors, props, stairs, basements, collision | Building scenes and test ground |
| **SCENE/Scene_AI.tscn** | Pathfinding, perception, investigation, combat and NPC simulation | AI actors, simple target/player stand-in, test geometry |
| **SCENE/Scene_Game.tscn** | Eventually instantiate integrated World, Player, AI and UI | Only systems ready for integration |

Each test scene should launch on its own. The World scene must not require a fully implemented Player scene; the Player must not assume the presence of a particular terrain. Provide small adapters (spawn placement, terrain collision, world queries, interaction, persistence) when integrating.

## Planned project layout

Three top-level ownership folders now exist: **[WORLD/](WORLD/README.md)**, **[PLAYER/](PLAYER/README.md)** and **[ASSETS/](ASSETS/README.md)**. The other paths below are **planned**, not claims of implemented scenes or scripts. Create feature folders/scenes as each milestone requires them; avoid empty placeholders.

~~~text
ben-final/
├── README.md
├── AGENTS.md
├── CodePreferences.md
├── ben-final.csproj             [created]
├── SCENE/                      [created]
│   ├── README.md               [created]
│   ├── Scene_Player.tscn       [created]
│   ├── Scene_World.tscn        [planned]
│   ├── Scene_Buildings.tscn    [planned]
│   ├── Scene_AI.tscn           [planned]
│   └── Scene_Game.tscn         [planned]
├── PLAYER/                     [created]
│   ├── README.md                [created]
│   ├── Player.tscn              [created]
│   ├── PlayerController.cs      [created]
│   ├── Input/
│   │   └── PlayerInput.cs       [created]
│   ├── Movement/
│   │   ├── PlayerMovement.cs    [created]
│   │   └── PlayerStance.cs      [created]
│   ├── Configuration/
│   │   ├── PlayerSettings.cs    [created]
│   │   └── PlayerMaster.tres    [created]
│   ├── Weapons/                 [planned]
│   └── Interaction/             [planned]
├── WORLD/                      [created]
│   ├── README.md                [created]
│   ├── WORLD_AUTHORING.md       [created]
│   ├── World.tscn               [planned]
│   ├── Generation/              [planned]
│   │   ├── DensityGenerator.cs  [planned]
│   │   ├── MarchingCubes.cs     [planned]
│   │   └── BiomeGenerator.cs    [planned]
│   ├── Streaming/               [planned]
│   │   ├── ChunkManager.cs      [planned]
│   │   └── TerrainLOD.cs        [planned]
│   └── Vegetation/              [planned]
├── Buildings/                  [planned]
├── AI/                         [planned]
├── ASSETS/                     [created]
│   └── README.md                [created]
├── Configuration/
└── Documentation/
~~~

Prefer one clearly owned folder per subsystem, reusable sub-scenes, configuration resources for tunable values, and a brief feature README when a subsystem becomes real. Keep scene and script names stable once used by other systems.

## Planned integrated scene ownership

~~~text
MainGame
├── World                    (WORLD/World.tscn)
│   ├── TerrainRoot
│   ├── VegetationRoot
│   └── EnvironmentRoot
├── WorldActorsRoot
│   ├── PersistentEntitiesRoot
│   │   ├── Player           (PLAYER/Player.tscn)
│   │   └── AIActors
│   └── PersistentPropsRoot
│       ├── Structures
│       └── Objects
├── GameplayHUD
├── DebugUIRoot
└── SystemsRoot
~~~

These are **ownership concepts**, not nodes that must be created on day one. Keep streaming, persistent actors/props, vegetation, terrain and shared services distinct. Systems should be configurable through exports and resources rather than hard-coded test-scene node paths.

## Custom procedural world — technical direction

### 1. Density-based geometry and marching cubes

- Use deterministic world-space noise and an editable **3D signed density function** to classify solid versus empty space.
- Marching cubes samples the eight corners of each cell; when density changes sign, interpolate edge positions and emit triangles. Adjacent chunks must sample matching border coordinates.
- Begin with surface hills, then test caves, overhangs, cliffs and underground volumes. The generation algorithm alone does not provide attractive terrain: separate density shaping, biome rules, materials and vegetation.
- Do not automatically assume marching cubes is the cheapest approach everywhere. Evaluate **hybrid heightfield surface + volumetric cave regions** if measurements show it is advantageous.

### 2. Chunk layout and quadtree/LOD

- Proposed first experiment: **32 m chunk width**, with **32 × 32 × 32 m** volumetric chunks when needed; at 1 m spacing a complete voxel cell chunk needs **33 samples per axis** for shared boundaries.
- These are **starting settings**, not permanent constraints. Chunk coordinates and density sampling must be deterministic and world-space-based.
- Use horizontal region management (a quadtree is a candidate) for surface visibility/LOD and 3D chunk indexing for underground volumes. Compare an octree only if justified.
- Implement explicit LOD transition handling, skirt/transvoxel-style stitching or another validated crack-prevention method. Coarse/fine neighbour transitions require their own solution.
- Keep terrain mesh LOD, collision residency, vegetation LOD, building/object streaming and AI simulation as separate concerns.

### 3. Multithreading and streaming

- Generate density buffers, meshes and placement data on background workers when safe.
- Integrate scene-tree nodes and resources on the appropriate Godot thread; avoid unsafe worker access to active scene nodes.
- Use generation queues, cancellation/stale-result rejection, bounded worker budgets and unload rules so movement does not cause memory or CPU spikes.
- Stream the **area around the player**; never allocate a complete 32 km volumetric grid. Persist only seeds/settings and player-authored changes where possible.

### 4. World scale, persistence and rendering

- **Initial proof of concept:** approximately **512 × 512 m**, expandable after a stable chunked prototype.
- **Long-term ambition:** up to **32 km × 32 km**, subject to performance and actual game needs; not a guaranteed milestone.
- Plan for large-world coordinate precision early (for example, floating origin or a suitable large-world build strategy), but implement only when needed.
- Use suitable materials, terrain blending, fog, natural lighting, MultiMesh vegetation instancing and authored tree/grass LODs. The marching-cubes algorithm itself does **not** generate lush visuals.
- Keep generated terrain reproducible by seed. Save terrain edits, structures and persistent actors separately, with a versioned format.

## Authoring and ownership guides

- **[WORLD/WORLD_IMPLEMENTATION_PLAN.md](WORLD/WORLD_IMPLEMENTATION_PLAN.md)** — current terrain implementation, verified user-run MeshStep 1/2 results, unresolved startup spikes and prioritized Codex continuation tickets.
- **[WORLD/WORLD_AUTHORING.md](WORLD/WORLD_AUTHORING.md)** — landscape goals; deterministic geography, marching cubes, vegetation, biomes, caves, chunk streaming, long-distance presentation, seams, performance and verification.
- **[WORLD/README.md](WORLD/README.md)** — WORLD ownership, FPS-independent test controls, mesh-step/collision settings and the BW-W1 performance comparison procedure.
- **[PLAYER/README.md](PLAYER/README.md)** — standalone player/FPS architecture and test-scene isolation.
- **[SCENE/README.md](SCENE/README.md)** — test-room ownership, F6 run instructions and planned separate test scenes.
- **[ASSETS/README.md](ASSETS/README.md)** — imported model, texture and audio organization; source versus reusable scene prefabs.

The terrain vision is **coherent large-scale geography**, not random noisy hills: grassy foregrounds, forested valleys, jagged distant mountain ranges, optional shorelines and snow at altitude. Marching cubes creates surfaces; separate geography, biome, materials, vegetation, water and atmospheric systems create the result. The world authoring file is the detailed guide.

## Development milestones

| ID | Milestone | Deliverable | State |
| --- | --- | --- | --- |
| **BW-00** | Repository and project foundation | Documentation and .NET project; local project.godot still needs a GitHub commit | **In Progress** |
| **BW-W1** | Marching-cubes proof of concept | Surface density, indexed mesh, shared-height normals, separate mesh/collision timing, debug spike counters and resolution comparison | **Awaiting Validation** |
| **BW-W2** | Adjacent chunk generation | Bounded 3×3 static patch; world-space sampling and matched boundaries with adjustable uniform mesh step, seam check pending | **Awaiting Validation** |
| **BW-W3** | Natural geography and caves | Coherent terrain regions, height variation, ridges, density shaping, cliffs, caves, overhangs and tunable seed | **Planned** |
| **BW-W4** | Streaming and LOD | Loading/unloading, distance LOD and transition geometry; initial preview has only a one-worker finite generation queue | **Planned** |
| **BW-W5** | Forest and atmosphere pass | Terrain materials, grass, pine trees, vegetation LOD/instancing, lighting/fog | **Planned** |
| **BW-F1** | Isolated FPS test | Reusable player, arrow keys, inverted mouse, RMB jump, right-Ctrl crouch and graybox test room | **Awaiting Validation** |
| **BW-B1** | Isolated building test | Reusable building/interior scenes and collision; investigate ground and basement interfaces | **Planned** |
| **BW-A1** | Isolated AI test | An NPC test actor with basic movement/perception in a dedicated arena | **Planned** |
| **BW-I1** | First integration | Instance World and Player together; spawn on terrain and navigate with collision | **Planned** |
| **BW-P1** | Long-range persistence/performance | Save/load edits and actors; larger traversal tests; profiling toward world-scale targets | **Planned** |

### Recommended first implementation

Start with **BW-00** and **BW-W1**. Run SCENE/Scene_World independently with its free-fly camera. Verify mesh appearance, triangle winding, collision and chunk seams before introducing caves, LOD, streaming, vegetation or 32 km scale. Test FPS features in SCENE/Scene_Player independently, without coupling them to Scene_World.

Advance to later milestones only after the previous implementation is reviewed. A Git commit or a compiling project is not proof of acceptable visual quality, correct caves or smooth FPS.

### BW-W1 performance comparison

The pre-optimization screenshot recorded **21,308 triangles across nine chunks, 74.5 ms CPU meshing and 129.4 ms combined main-thread setup/collision work**. The new test separates mesh and collision costs, restricts collision to the centre chunk by default, reports individual chunk timings, and offers `MeshStep = 1 / 2 / 4` with **identical physical chunk dimensions**. These optimizations have **not yet been built or benchmarked locally**; follow [WORLD/README.md](WORLD/README.md) to run fair comparisons.

## Architecture and integration contracts

- **WORLD owns terrain**: generation, mesh/collision residency, world seed, biomes and world queries such as surface height, solid/empty samples and safe spawn positions. Follow [WORLD/WORLD_AUTHORING.md](WORLD/WORLD_AUTHORING.md) for design/implementation guidance.
- **PLAYER owns FPS behavior**: movement, camera, input, equipment and interactions. It should consume world information via explicit contracts, not by knowing the test level hierarchy. Follow [PLAYER/README.md](PLAYER/README.md).
- **Buildings own their geometry**: authored/procedural structure, interiors, colliders, interactable objects and optional requests for terrain excavation/placement. World integration resolves those requests.
- **AI owns its behavior**: perception, navigation and actions; level geometry and spawn handling come from its host scene.
- **MainGame composes subsystems**: instances the scenes and connects interfaces/signals. Avoid direct hard-coded cross-feature node paths.
- **Saving** stores world seed, generated-world version, terrain modifications, actors and objects separately. Distinguish deterministic content from mutable state.

## Performance and validation principles

Aim for **smooth FPS without stalling when crossing chunk boundaries**, but benchmark actual frame time, memory and chunk-generation latency rather than declaring an arbitrary FPS guarantee.

- Prefer jobs/events over idle per-frame loops; bound worker queues and mesh upload work.
- Avoid per-blade grass nodes, unnecessary collisions on distant geometry, and high-detail volumetric data outside the active region.
- Test cracks at both same-resolution and differing-LOD borders, cave normals, seams, collider readiness, teleport/fast traversal, unloading and re-entry.
- Maintain a debug view of active chunks, queued jobs, LOD levels, generation timings and rough memory use.
- Keep checks focused; owner performs visual/FPS checks in Godot. Report clearly what was and wasn't verified.

## Reuse from existing repositories

- **[War Machine](https://github.com/ben0kay/war-machine):** FPS/player systems, interactions, buildings/props, AI experiments, chunk streaming patterns, root ownership, and development lessons.
- **[Final Machine](https://github.com/ben0kay/final-machine):** vegetation assets and LODs, graphics/forest authoring lessons, scene/configuration conventions, and the source of the initial agent/code-preference documents.
- **[Terra Machine](https://github.com/ben0kay/terra-machine):** additional foliage, performance and world-art experiments.

**Godot configuration:** Ben Final already has a committed `project.godot` with the `ben-final` assembly name. The original settings remain intact, with the `C#` feature tag added. `ben-final.csproj` provides the .NET project. Build and runtime validation in the Godot editor are still required.

**Do not port Terrain3D or Voxel Tools implementations automatically.** Transfer isolated assets or reusable logic only after examining dependencies, licensing, coordinate systems, and compatibility with Ben Final's custom generator. Assets/code have **not** been copied into this repository as part of these planning documents.

## Instructions for future contributors and AI agents

Read **AGENTS.md**, **CodePreferences.md**, this README, **WORLD/WORLD_AUTHORING.md** for any terrain/biome/vegetation/world change, and the owning feature documents before making changes. Respect the planned-versus-implemented distinction, feature ownership, minimal testing policy, and project branch permissions. Propose focused steps, avoid unrelated rewrites, and keep project plans updated as milestones become real.
