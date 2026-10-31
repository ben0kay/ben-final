# Code Preferences — Ben Final

> **Applies to:** `ben0kay/ben-final` **only**. Adapted from Final Machine's coding conventions. Read [README.md](README.md) first.  
> **Development branch:** `main` unless the owner explicitly names another branch. Never inherit old repository branch policies.  
> **References:** [final-machine](https://github.com/ben0kay/final-machine), [terra-machine](https://github.com/ben0kay/terra-machine) and [war-machine](https://github.com/ben0kay/war-machine) are **reference projects**, not the target repository.

**Custom procedural terrain-first project.** The planned world generator uses Godot 4 C#, deterministic 3D density data, marching cubes, chunk streaming and custom LOD. No terrain plugin is required. Reference repos may contain obsolete paths and architecture: never import War Machine's branch policy, Final Machine's Terrain3D dependence, Terra Machine's Voxel Tools architecture or assumed files.

## Before every AI task

1. Confirm repo, branch, authorized scope and actual files; read `AGENTS.md`, root README, this file, relevant feature docs and code.
2. Prefer **Godot 4 .NET/C#**, native scenes/resources and our **custom procedural terrain code**; do not install Terrain3D, Voxel Tools or alternative terrain addons unless the owner explicitly authorizes them.
3. Preserve unrelated work; no unsolicited migrations, scene restructuring or gameplay additions.
4. Prefer **one commit per authorized job**; report SHA/files. Push authorized work, never extra unrelated work. No forced resets/checkouts or overwriting others' changes.
5. **Brief, proportional checks only:** focused source review, basic build/essential test when practical; owner handles extensive visual/FPS validation. Mark unverified results.

## Coding style and organization

- Put a brief explanatory comment at the top of every new script.
- Prefer comments **above** methods, with a `// =========================================================` divider for easy navigation.
- Use `#region` / `#endregion` to organize substantial related method groups when helpful.
- Favor moderately **vertically compact**, readable C# rather than sprawling or overly abstract code.
- Major feature folders need a small `README.md` (responsibility, data flow, configuration, limitations). Read before edits; update when behaviour changes.
- Use a grouped **master configuration resource** (`Configuration/<Feature>Master.tres` plus typed C# `Resource` when appropriate) for frequently adjusted knobs. Group exports in the Godot inspector.
- Keep modular, discoverable feature folders and reusable, clearly owned logic. Use shared helpers/plain `Node` helpers when useful; avoid unnecessary managers and abstraction.
- Keep gameplay/domain logic separate from addon/native integrations and imported visuals; allow art replacement without rewriting mechanics.

### Scene composition and world-root ownership (mandatory)

Keep test scenes independently runnable and gameplay systems reusable. The conceptual integrated scene layout is:

~~~text
MainGame
├── World (WORLD/World.tscn)
│   ├── TerrainRoot
│   ├── VegetationRoot
│   └── EnvironmentRoot
├── WorldActorsRoot
│   ├── PersistentEntitiesRoot
│   │   ├── Player
│   │   └── AIActors
│   └── PersistentPropsRoot
│       ├── Structures
│       └── Objects
├── GameplayHUD
├── DebugUIRoot
└── SystemsRoot
~~~

- This is a **planned ownership pattern**, not a demand to create empty placeholder nodes. Follow root README milestone status and instantiate only what's needed.
- **SCENE/Scene_Player.tscn** instances the reusable `PLAYER/Player.tscn` in a graybox; future **SCENE/Scene_World.tscn** can generate and inspect terrain with a standalone debug camera and without the Player. Keep the ownership folders as uppercase `WORLD/`, `PLAYER/`, `ASSETS/`; do not create lowercase variants. Building and AI test scenes stay equally independent. MainGame instances and connects the systems when ready.
- Spawn actors under persistent entity roots and structures/props under their respective roots, rather than scattering persistent objects under the World root. Separate save/stream/debug ownership.
- Use reusable instanced subsystem scenes and lightweight organizing `Node`/`Node3D` parents; **container-only nodes need no processing scripts**. Stable paths and explicit contracts are preferable to hard-coded cross-feature test-scene references.
- Batch foliage with MultiMesh/region ownership instead of thousands of per-blade scene nodes. Keep vegetation rendering, terrain generation, physics, props and actor residency independently manageable.

### Reference repository conventions

- **[Final Machine](https://github.com/ben0kay/final-machine):** vegetation models/LODs, scene conventions, materials, visual authoring and C# practice. **No implicit Terrain3D port.**
- **[Terra Machine](https://github.com/ben0kay/terra-machine):** foliage authoring, tree LOD/shadow budgets, instancing, shaders, performance, C# organization and landscape lessons. **No implicit Voxel Tools port.**
- **[War Machine](https://github.com/ben0kay/war-machine):** object catalogues, buildings/props, zombies/anatomy/AI, physics, master resources and C# documentation **as references only**, not active architecture/branch rules.
- Optional idea sources: [godot-tower-defense](https://github.com/ben0kay/godot-tower-defense), [isometric-new-2d](https://github.com/ben0kay/isometric-new-2d), [Ben_MonsterExpansion](https://github.com/ben0kay/Ben_MonsterExpansion).

## Performance-first implementation rules

**Ask before coding:** *What activates this feature? What happens while it is active? What makes it stop? What does it cost while idle?*

- **Inactive means dormant:** no `_Process` / `_PhysicsProcess` loop by default when no recurring work is necessary. Use signals, input events, `SetProcess(false)`, `SetPhysicsProcess(false)`, timers, explicit wake/sleep and effect shutdown where appropriate.
- Shared managers, reusable helpers, instancing, batched foliage, pooling and cached resources are preferred over huge numbers of individual per-frame node updates.
- Update changed shader/material settings on changes; do not rewrite the same uniforms every frame. Keep frequent paths allocation-conscious and avoid redundant scene-tree searches and resource loads.
- Only simulate nearby, relevant actors in detail. Design for future world/object streaming: register/unregister with region residency; do not scan the entire map each frame.
- Weather particles, fire, chopping effects, creature investigation, construction events etc. should have explicit activation and termination behavior. Continuous animation such as active wind/daylight may legitimately update, but minimize duplicate work.
- Terrain mesh LOD, region residency, vegetation LOD, collision/physics activation, AI simulation and object persistence are **separate concerns**. Do not claim custom mesh LOD automatically provides full region/object streaming.
- Report any meaningful FPS/memory measurement conditions; never fabricate improvements. Include wake/sleep, scaling strategy and unverified checks in each feature handoff.

## Authoring references and folder names

- For terrain, biomes, caves, shoreline, vegetation, chunk streaming, world LOD or procedural geography, read **[WORLD/WORLD_AUTHORING.md](WORLD/WORLD_AUTHORING.md)** before implementation; consult [WORLD/README.md](WORLD/README.md) for ownership.
- For FPS mechanics and reusable player scenes read **[PLAYER/README.md](PLAYER/README.md)**. Keep physical key/mouse bindings centralized in `PLAYER/Input/PlayerInput.cs`; movement and stance logic stay in `PLAYER/Movement/`. Independent test scenes belong in uppercase `SCENE/`.
- For GLB/GLTF imports, source textures, audio and asset-facing wrappers read **[ASSETS/README.md](ASSETS/README.md)**.
- The uppercase top-level names are intentional. Keep them exact across Git, paths and Godot references.

## Custom procedural world reliability

- Keep density sampling deterministic in world space, independent of chunk load order, camera position or worker schedule. Store world seed/configuration separately from edited terrain state.
- For a chunk of N marching-cubes cells along an axis, sample N+1 grid positions along that axis so adjacent chunks agree on shared boundary values. Test normals and winding consistently.
- Treat **same-resolution boundaries** and **different-LOD transitions** as different mesh problems; avoid promising crack-free LOD without transition geometry or another verified solution.
- Use bounded, cancellable worker jobs for density/mesh calculation; keep scene-tree mutations on the correct thread and reject results for chunks that are no longer wanted.
- Stream only nearby chunks. A 32 km × 32 km ambition is not permission to allocate a world-sized 3D grid. Consider large-world coordinate precision and efficient collision/vegetation residency.
- In the initial prototype, prefer a small test map (~512 × 512 m), 32 m chunk width as a tunable starting value, one simple hill and collision before adding caves/vegetation. The root README is the milestone authority.
- Keep any alternative/hybrid terrain research explicit and benchmark-driven; no unilateral plugin swap or migration.

## Assets, graphics and scene structure

- Prefer GLB/GLTF for authored art, retaining reusable game-facing `.tscn` prefabs; replace visuals without replacing gameplay contracts, collision and pivots/sockets.
- If generating geometry with C#, use an asset-local **`C/`** folder for mesh builders and an **`Artwork/`** sibling for future imported art, with `Configuration/` for editable settings where appropriate. `C/` is **not** where all C# gameplay scripts belong.
- Source-model triangle budgets are **starting guidance**, not mandatory engine limits. The old Terra Machine authoring guide suggests mature trees around 4k–6k triangles near and 1k–1.5k at a further LOD, with much smaller budget for grass. Test real frame time and shadow/alpha overdraw rather than judging triangles alone.
- Prefer simple colliders on trees and props unless precise contact matters. Separately consider static world geometry, interactive physics bodies and future simulation streaming.
- Use custom terrain materials, surface weights and biome blending appropriate to the selected mesh generator; do not blindly port Final Machine's Terrain3D configuration or old triplanar shaders.
- For caves, basements and overhangs, use a deliberate signed-density/3D-volume design where appropriate; test navigation, collision, mesh joins, terrain excavation and preservation of generated data. A hybrid heightfield/volumetric approach may be evaluated if measurements justify it.

## C# syntax / build hygiene

- Collection enumeration uses `foreach (var item in items)`, **not** invalid `for (var item in items)`.
- Indexed loops use `for (int i = 0; i < count; i++)`. Inspect newly added loops, types and Godot API names before committing.
- Prefer an initial `dotnet build` for authorized C# work if the Godot .NET SDK/environment is available. If not, perform focused source review and explicitly state **compilation not verified**. A successful Git push is not proof of a working scene.
- Calls into the Godot 4 .NET API must match the actual installed Godot version; inspect current APIs and avoid imaginary types, methods or thread-safety guarantees.

## Planning / handoff

Regular Chat: exploration, plans and implementation when authorized. Codex/Work: multi-file C#/scene integration; Copilot: delegated local checks. **Tool choice never grants task authorization.**

- For substantial work, propose short independently reviewable phases with IDs, current vs proposed behavior, owner paths, configuration, performance cost, prerequisites and acceptance checks.
- Do not automatically implement later phases. Use Planned / In Progress / Implemented / Awaiting Validation / Accepted and keep README statuses accurate.
- Keep tests proportional. If the owner wants local checks delegated to Copilot, hand off exact commands, prerequisites and expected results; do not imply checks were already run.
- Update owning documentation after authorized changes; remove superseded contradictions, mark future tasks clearly. Document significant fixes centrally in `LESSONS_LEARNED.md` per `AGENTS.md`.

**Reminder:** Ben Final is a **modular technical foundation**: build independent FPS, world, building and AI test scenes, integrate through reusable scenes/contracts, and prioritize a reliable custom procedural terrain prototype before world-scale expansion. Do not rush unapproved gameplay, vehicles, crafting or world-scale changes.
