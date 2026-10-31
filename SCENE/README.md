# SCENE — Independent Test Worlds

Test scenes instance reusable feature scenes; gameplay and generation code remain in their owning folders.

- **[Scene_Player.tscn](Scene_Player.tscn)** — graybox FPS room with an instanced `res://PLAYER/Player.tscn`; **awaiting local Godot validation**.
- **[Scene_World.tscn](Scene_World.tscn)** — marching-cubes terrain preview; **9×9 static chunks spanning 576×576 m**, sky/sun, faster free-flying debug camera and separated CPU/mesh/collision timing. Mesh resolution, chunk radius and collision range are in WORLD/Configuration/WorldMaster.tres; **larger preview awaiting local validation**.
- `Scene_Buildings.tscn`, `Scene_AI.tscn`, `Scene_Game.tscn` — **planned**, not created.

### Scale reference

The world preview instances **one low-poly 14 m tall pine-shaped landmark** from `WORLD/Debug/WorldScaleTree.tscn` at x=8, z=90, visible ahead of the starting camera. `WorldScaleTree.cs` samples the **same seed and density-height function** as the terrain and positions the trunk base on the surface when the scene loads. It has **no collider or per-frame updates**, and is not part of the eventual procedural forest system. Move the `ScalePine_14m` node in `Scene_World.tscn` to test scale elsewhere.

## Running the World preview

Build C# in Godot 4.7.2 .NET, open `SCENE/Scene_World.tscn` and press **F6**. Mouse looks around (inverted vertical by default); WASD/arrow keys fly, Q/E lower/raise, Shift moves faster, Escape releases the cursor and left click recaptures it.

The test's generation/performance settings are in `WORLD/Configuration/WorldMaster.tres`. Change `MeshStep` from 1 to 2 to compare triangle counts at the same physical world size, or adjust `CollisionChunkRadius` (default 0, centre only). Detailed methodology is in [WORLD/README.md](../WORLD/README.md). All terrain code belongs to `WORLD/`; `PLAYER/` is not required for the world test.

Use F6 for either independent test scene. To run either with F5, choose it in **Project Settings → Application → Run → Main Scene**. Keep the existing `project.godot` untouched otherwise.
