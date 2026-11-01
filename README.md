# Ben Final

**Repository:** ben0kay/ben-final  
**Engine:** Godot 4 .NET / C# (existing Ben Final project.godot preserved; C# support configured)  
**Project direction:** **Finite island survival, exploration and crafting game**. The player is stranded on an island surrounded by water; the long-term goal is to scavenge, gather and craft the components required to assemble a working plane and escape by taking off. The island is **semi-authored / procedural**, with a repeatable seed and deliberate placement of important locations. This is the target design, **not implemented gameplay**.

**Technical status:** The owner has run a 27×27 (1,728 m) static terrain preview with 729 chunks. The terrain is currently an unbounded-in-shape rectangular test patch, **not yet an island**, and looks too plain in places. An F1 panel can request 1–100 chunks per side, change the seed and tune seven geography sliders; recent input and terrain revisions await local validation. **Water, coastlines, procedural forests, placed building sites, crafting, the plane objective, runtime chunk streaming, mixed-distance LOD and caves are not implemented.**

## Game vision — escape the island

**Ben Final is a first-person survival, exploration and crafting game set on one finite island surrounded by ocean.** The island should feel like a believable place, not a limitless randomly generated sandbox. The player explores the wilderness and selected points of interest, scavenges useful materials and equipment, and gradually builds what is needed to **repair or assemble a functional plane and take off to escape the island**.

The intended experience combines open-ended island exploration with a clear long-term objective:

- **Explore:** Walk through distinct natural areas, shorelines, forests, hills and mountains; discover deliberately positioned landmarks and useful sites.
- **Gather and scavenge:** Obtain raw materials, supplies, tools and recoverable components from nature and points of interest.
- **Craft and assemble:** Progress from basic survival and utility crafting to specialist components needed for the plane. The exact recipes, plane type, inventory and assembly mechanics are **to be designed**, not predetermined here.
- **Prepare for escape:** Locate or prepare a suitable take-off area, complete the aircraft, obtain whatever it needs to operate, and make a successful take-off the main end goal.

The world is **semi-authored / procedural**, not wholly random each playthrough. Use procedural systems to create natural variation and reduce repetitive placement work, while authoring the terrain composition, essential locations and progression deliberately. Survival pressure, weather, wildlife, AI encounters, caves, basements and more advanced simulation are possible later additions, **not assumed features**.

The project continues to use a **custom Godot 4 C# terrain foundation** (marching cubes currently), with streaming, level of detail and background generation as engineering goals. World technology exists to support the island game, not as an end in itself. Existing War Machine and Final Machine systems are reference material and should only be adopted selectively.

### World design contract: fixed seed, controlled iteration

| Element | Intended approach | Authoring responsibility |
| --- | --- | --- |
| **Island terrain** | Generate from a **fixed, reproducible seed** and editable geography parameters. Iterate with AI until a layout is approved. | Owner chooses overall island shape, regions, landmarks and acceptable terrain revisions. AI implements the generator and requested edits. |
| **Coast and ocean** | One finite landmass, with varied coastline, beaches, rocky shores and surrounding water. | Island boundary and shoreline design are intentional; water rendering and sea level will be separate systems. |
| **Biomes and vegetation** | Procedural placement **within intentionally defined zones**—e.g., dense forest here, short grass/ferns there, clearing around a site. | Owner chooses where regions belong; AI authors density, species mixes, exclusion rules and deterministic scatter code. |
| **Buildings and points of interest** | Selected **authored locations** with reusable building/site scenes; procedural dressing where useful. | Owner decides important positions and purpose; AI builds or adapts prefabs and integrates foundations/terrain clearance. |
| **Gameplay and progression** | Scavenging, crafting, upgrading and plane assembly, leading to take-off. | Design deliberately rather than assigning essential parts by uncontrolled random chance. |

**Iteration policy:** While designing the island, change the seed or geography parameters and regenerate until the owner approves it. Once a layout is selected, **lock the canonical seed and terrain-generation version**. After that, prefer targeted, persistent authored overrides (terrain shaping, forest regions, building sites, cleared areas) over globally rerolling the world. Edits must not unexpectedly relocate structures, invalidate the coastline or strand progression items.

**Finite does not mean fully loaded.** The world has a defined boundary and a chosen size (still TBD), but terrain, vegetation and points of interest should eventually stream around the player as they explore. A 50×50 or 100×100 F1 grid is currently just a **static terrain stress test**, not the proposed final island dimensions.

### Key design boundaries

- The island will be **single and finite**, surrounded by water, rather than an infinite world or automatically expanding mainland.
- **Hand-authored direction comes first:** use procedural generation to fill and vary regions, never to erase intended roads, clearings, building plots, important landmarks or the escape objective.
- Keep **terrain topology**, **biome/foliage placement**, **building/site placement** and **quest/item progression** in separate, deterministic layers. An AI should be able to revise one without rebuilding everything.
- Keep gameplay-critical locations accessible and ensure required plane components remain obtainable. Randomness can vary optional loot, encounters and environmental detail, but must not make the main objective impossible.
- The ocean is a natural world boundary, not proof of implemented swimming, boats, underwater simulation or an invisible wall. Those rules can be decided later.

## Core development approach: independent scenes

Build each subsystem in its own scene and test environment; bring them together later through **scene instancing and shared contracts**, not by merging unrelated source code or making one monolithic test scene.

| Development scene | Primary purpose | Dependencies |
| --- | --- | --- |
| **SCENE/Scene_Player.tscn** | Arrow-key FPS, inverted mouse look, RMB jump and right-Ctrl crouch | Instances reusable PLAYER/Player.tscn; graybox with collision (awaiting validation) |
| **SCENE/Scene_World.tscn** | Static world test with validated 1–100 NxN chunks, F1 seed + 7 geography sliders, regeneration and fly camera | Instances WORLD/World.tscn; no Player required; new panel/landforms await validation |
| **SCENE/Scene_Buildings.tscn** | Buildings, interiors, doors, props, stairs, basements, collision | Building scenes and test ground |
| **SCENE/Scene_AI.tscn** | Pathfinding, perception, investigation, combat and NPC simulation | AI actors, simple target/player stand-in, test geometry |
| **SCENE/Scene_Game.tscn** | Eventually integrate the island, player, authored sites, gathering/crafting and escape objective | Only systems ready for integration; **planned** |

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
│   ├── Scene_World.tscn        [created — large static flyover]
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
│   ├── WORLD_IMPLEMENTATION_PLAN.md [created]
│   ├── World.tscn               [created]
│   ├── WorldPreview.cs          [created]
│   ├── Generation/              [created]
│   │   ├── DensityGenerator.cs  [created]
│   │   ├── MarchingCubesMesher.cs [created]
│   │   └── BiomeGenerator.cs    [planned]
│   ├── Configuration/           [created]
│   │   ├── WorldSettings.cs     [created]
│   │   └── WorldMaster.tres     [created]
│   ├── Debug/                   [created]
│   ├── Materials/               [created]
│   ├── Streaming/               [planned]
│   │   ├── ChunkManager.cs      [planned]
│   │   └── TerrainLOD.cs        [planned]
│   ├── Island/                  [planned: land mask, coast, water]
│   ├── Regions/                 [planned: authored biome/site zones]
│   └── Vegetation/              [planned: zone-based scatter]
├── Buildings/                  [planned: authored sites/prefabs]
├── Gameplay/                   [planned: gather/craft/plane escape]
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

### 1. Island geography and density-based geometry

- Plan a **finite island shape** with a coastline/shoreline mask, sea-level reference and intentional large landforms (plains, hills, forests, mountains), instead of leaving the default terrain as an endless rectangular patch. A generated island coastline is **planned**, not in the current code.
- Use deterministic world-space noise and an editable **3D signed density function** to classify solid versus empty space.
- Marching cubes samples the eight corners of each cell; when density changes sign, interpolate edge positions and emit triangles. Adjacent chunks must sample matching border coordinates.
- Begin with surface hills, then test caves, overhangs, cliffs and underground volumes. The generation algorithm alone does not provide attractive terrain: separate density shaping, biome rules, materials and vegetation.
- Do not automatically assume marching cubes is the cheapest approach everywhere. Evaluate **hybrid heightfield surface + volumetric cave regions** if measurements show it is advantageous.

### 2. Chunk layout and quadtree/LOD

- The **current preview uses 64 m horizontal chunks** (`CellsPerChunk=32`, `CellSize=2 m` by default). Preserve these as tunable technical settings; the final island scale and resolution are undecided. If later using true volumetric 3D chunks, sample shared boundaries consistently (N+1 samples per axis).
- These are **starting settings**, not permanent constraints. Chunk coordinates and density sampling must be deterministic and world-space-based.
- Use horizontal region management (a quadtree is a candidate) for surface visibility/LOD and 3D chunk indexing for underground volumes. Compare an octree only if justified.
- Implement explicit LOD transition handling, skirt/transvoxel-style stitching or another validated crack-prevention method. Coarse/fine neighbour transitions require their own solution.
- Keep terrain mesh LOD, collision residency, vegetation LOD, building/object streaming and AI simulation as separate concerns.

### 3. Multithreading and streaming

- Generate density buffers, meshes and placement data on background workers when safe.
- Integrate scene-tree nodes and resources on the appropriate Godot thread; avoid unsafe worker access to active scene nodes.
- Use generation queues, cancellation/stale-result rejection, bounded worker budgets and unload rules so movement does not cause memory or CPU spikes.
- Stream the **needed area of the finite island around the player**; do not allocate its entire volumetric terrain at runtime merely because the world has an edge. Persist the locked seed/configuration, approved authored overrides and mutable player progress.

### 4. Finite island scale, persistence and rendering

- **Map design:** one bounded island surrounded by ocean. Choose its final physical dimensions based on island composition, travel time and performance—not an arbitrary 32 km or 100×100 chunk target. The ocean extent can be visually broader than the playable land.
- **Iteration:** prototype geography using the existing F1 size/seed/sliders, add island/coastline controls, then approve and lock a canonical layout. Changing a test-grid width must not secretly change the approved coastline or move authored locations.
- **Persistence:** save the approved world seed, generation/schema version, terrain and region authoring overrides, placed-site identities, items removed/collected, crafting progress and plane assembly state separately. Generated scenery is reconstructible; authored or player-changed state must survive unload/reload.
- **Rendering:** use appropriate terrain materials, shoreline/water treatment, fog, lighting, instanced procedural foliage, vegetation LOD and section-based streaming. The marching-cubes algorithm itself does **not** generate forests, water or good art.
- Consider floating-origin/large-world precision only if the **chosen finite island scale** actually requires it. Prevent unintended exploration outside the authored boundary without requiring huge invisible land areas.

## Authoring and ownership guides

- **[WORLD/WORLD_IMPLEMENTATION_PLAN.md](WORLD/WORLD_IMPLEMENTATION_PLAN.md)** — current terrain implementation, verified user-run MeshStep 1/2 results, unresolved startup spikes and prioritized Codex continuation tickets.
- **[WORLD/WORLD_AUTHORING.md](WORLD/WORLD_AUTHORING.md)** — landscape goals; deterministic geography, marching cubes, vegetation, biomes, caves, chunk streaming, long-distance presentation, seams, performance and verification.
- **[WORLD/README.md](WORLD/README.md)** — WORLD ownership, FPS-independent test controls, mesh-step/collision settings and the BW-W1 performance comparison procedure.
- **[PLAYER/README.md](PLAYER/README.md)** — standalone player/FPS architecture and test-scene isolation.
- **[SCENE/README.md](SCENE/README.md)** — independent test-scene ownership, F6 and F1 regeneration controls.
- **[ASSETS/README.md](ASSETS/README.md)** — imported model, texture and audio organization; source versus reusable scene prefabs.

The terrain vision is **a coherent finite island**: varied coastline and ocean, accessible lowlands, forest/grass/fern zones, hills and mountain silhouettes, with deliberate clearings and authored points of interest. Marching cubes creates surfaces; separate geography, biome, water, vegetation and site-authoring systems create the world. The existing world authoring file is a technical reference; the island vision and gameplay goal in this root README take precedence if older exploratory ambitions conflict.

## Development milestones

| ID | Milestone | Deliverable | State |
| --- | --- | --- | --- |
| **BW-00** | Repository and project foundation | Documentation and .NET project; local project.godot still needs a GitHub commit | **In Progress** |
| **BW-W1** | Marching-cubes proof of concept | Surface density, indexed mesh, shared-height normals, separate mesh/collision timing, debug spike counters and resolution comparison | **Awaiting Validation** |
| **BW-W2** | Adjacent chunk generation | Static centre-out NxN grid (1–100 chunks per side) with F1 validated world-size input and safe regeneration; seam checks pending | **Awaiting Validation** |
| **BW-W3A** | Composed geography | Original pass owner-tested (too flat + wall of spikes). New broad rolling hill masses, mountain coverage mask and seven shaping sliders implemented | **Awaiting Validation** |
| **BW-W3B** | True 3D caves and overhangs | Full volumetric density, vertical chunk ownership, cave entrances, editable terrain and basements | **Planned** |
| **BW-W4** | Streaming and LOD | Loading/unloading, distance LOD and transition geometry; initial preview has only a one-worker finite generation queue | **Planned** |
| **BW-W5** | Forest and atmosphere pass | Terrain materials, grass, pine trees, zone-based procedural ferns/undergrowth, vegetation LOD/instancing, lighting/fog | **Planned** |
| **BW-W6** | Finite island and ocean | Approved island land mask, sea level, beaches/rocky shores, coast-to-water transition and world boundary | **Planned** |
| **BW-W7** | Region authoring | Author-defined forests, meadows, clearings and no-spawn zones; deterministic foliage based on region masks | **Planned** |
| **BW-F1** | Isolated FPS test | Reusable player, arrow keys, inverted mouse, RMB jump, right-Ctrl crouch and graybox test room | **Awaiting Validation** |
| **BW-B1** | Isolated building test | Reusable building/interior scenes and collision; investigate ground and basement interfaces | **Planned** |
| **BW-B2** | Authored island sites | Owner-directed building/landmark locations, site footprints and ground adjustments; stable site identities | **Planned** |
| **BW-A1** | Isolated AI test | An NPC test actor with basic movement/perception in a dedicated arena | **Planned** |
| **BW-I1** | First integration | Instance World and Player together; spawn on terrain and navigate with collision | **Planned** |
| **BW-G1** | Gather and craft | Scavenging, resources, item components, recipe/progression scaffolding and inventory integration | **Planned** |
| **BW-G2** | Plane escape objective | Required aircraft components, assembly/repair, readiness checks, usable take-off area and escape outcome | **Planned** |
| **BW-P1** | Long-range persistence/performance | Save/load edits and actors; larger traversal tests; profiling toward world-scale targets | **Planned** |

### Recommended next implementation sequence

Keep the independent `SCENE/Scene_World.tscn` and `SCENE/Scene_Player.tscn` tests. First validate the current generator and establish bounded **BW-W4 terrain streaming** for manageable performance. Next design **BW-W6 island geometry and shoreline/water** by iterating on a fixed seed. Define authored region/site contracts (**BW-W7/BW-B2**) before filling the island with foliage or important buildings. Integrate player traversal, gathering/crafting and the plane escape objective in separate small, testable milestones. **These are recommendations, not authorization to implement all phases.**

Advance to later milestones only after the previous implementation is reviewed. A Git commit or a compiling project is not proof of acceptable visual quality, correct caves or smooth FPS.

### BW-W1 performance comparison

The original **96×96 m** pre-optimization screenshot recorded **21,308 triangles across nine chunks, 74.5 ms CPU meshing and 129.4 ms combined main-thread setup/collision work**. The updated test separates mesh and collision costs, restricts collision to the centre chunk by default, reports individual chunk timings, and offers `MeshStep = 1 / 2 / 4` with **identical physical chunk dimensions**. The previous large-world preset used **81 chunks (9×9), 64 m each, or 576×576 m**. The current preset is **729 chunks (27×27), 64 m each, or 1,728×1,728 m**, nine times the land area without reducing sampling resolution. Earlier timings must not be compared directly to either larger configuration. The owner has now run the 27×27 preset successfully; see [WORLD/README.md](WORLD/README.md) for that screenshot baseline and the new editable controls.

## Architecture and integration contracts

- **WORLD owns the finite island**: deterministic land/coast generation, ocean/world boundary, mesh/collision residency, region definitions and world queries such as surface height, solid/empty samples and safe spawn positions. Follow [WORLD/WORLD_AUTHORING.md](WORLD/WORLD_AUTHORING.md) for design/implementation guidance.
- **PLAYER owns FPS behavior**: movement, camera, input, equipment and interactions. It should consume world information via explicit contracts, not by knowing the test level hierarchy. Follow [PLAYER/README.md](PLAYER/README.md).
- **Buildings own their geometry**: authored/procedural structures, interiors, colliders and interactable objects. Stable island-site placement records/footprints are separate from reusable prefab definitions; World integration resolves site terrain clearance or excavation.
- **AI owns its behavior**: perception, navigation and actions; level geometry and spawn handling come from its host scene.
- **MainGame composes subsystems**: instances the scenes and connects interfaces/signals. Avoid direct hard-coded cross-feature node paths.
- **Gameplay owns progression**: loot/gather/craft interactions, availability of required aircraft components, assembly state and successful escape/take-off checks. The world provides locations and resources but should not hide critical progress behind unbounded randomness.
- **Saving** stores the locked world seed and generator version, region/site authoring overrides, terrain modifications, actors/objects, harvested resources and plane progress separately. Distinguish deterministic content from mutable state.

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
