# ASSETS — Imported Art and Media

**Status:** Folder/ownership guide only. No art, model, texture, audio or imported resource has been copied yet.

`ASSETS/` is the central home for **imported source assets** and shared visual/audio media used across WORLD, PLAYER, buildings and future game features.

## Intended organization (create only as needed)

~~~text
ASSETS/
├── README.md                 [exists]
├── Models/                  [planned]
│   ├── Vegetation/          [planned]
│   ├── Rocks/               [planned]
│   ├── Structures/          [planned]
│   ├── Props/               [planned]
│   └── Characters/          [planned]
├── Textures/                [planned]
├── Materials/               [planned]
├── Audio/                   [planned]
└── Effects/                 [planned]
~~~

Keep recognizable file names, provenance/licensing notes where needed, all dependencies (GLTF/GLB + required textures), and stable import paths. Prefer **GLB/GLTF** for authored models and reusable Godot `.tscn` wrappers in the relevant *feature* folder when the asset requires gameplay behavior, collisions, sockets, scripts, configurable LOD or spawn interfaces. A source asset is not automatically a complete game-ready prefab.

Do not overwrite user-authored meshes, change source scale/pivots or rename imported dependencies without checking references. Imported tree/grass LODs may be reused from Final Machine after explicit approval and compatibility checks; nothing was copied by creating this folder.

Terrain generation code and procedural density data belong in `WORLD/`, not `ASSETS/`. FPS logic belongs in `PLAYER/`, not `ASSETS/`.

Read [root README](../README.md), [AGENTS](../AGENTS.md) and [CodePreferences](../CodePreferences.md) before implementing a new asset workflow.
