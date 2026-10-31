# WORLD — World Generation and Rendering

**Status:** Documentation/architecture only. No world scene or generator has been implemented in Ben Final yet.

**Primary authoring guide:** [WORLD_AUTHORING.md](WORLD_AUTHORING.md). Read it before changing terrain, biome rules, caves, streaming, chunk LOD, vegetation, water, lighting or world-scale systems.

## Responsibility

`WORLD/` will own deterministic geography and 3D density sampling, marching-cubes mesh construction, chunk residency, LOD transitions, terrain collision, world coordinate conversion, vegetation placement, biome/material weights, environment effects and terrain-change integration.

This folder **does not own the FPS player, weapons or AI behavior**. The planned `SCENE/Scene_World.tscn` should be runnable with a standalone free-fly/debug camera; future `SCENE/Scene_Game.tscn` can instance the reusable `WORLD/World.tscn` alongside `PLAYER/Player.tscn`.

## Build strategy

Begin with a single deterministic mesh and collision (**BW-W1**); then adjacent chunks (**BW-W2**), coherent large-scale geography and caves (**BW-W3**), streaming and LOD (**BW-W4**) and visual landscape/vegetation work (**BW-W5**). All are **planned** until implemented and validated.

Keep generated terrain data separate from user-authored assets and permanent world edits. See the [root plan](../README.md) for milestone status and repository rules.
