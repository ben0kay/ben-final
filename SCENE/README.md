# SCENE — Independent Test Worlds

Test scenes instance reusable feature scenes; gameplay and generation code remain in their owning folders.

- **[Scene_Player.tscn](Scene_Player.tscn)** — graybox FPS room with an instanced `res://PLAYER/Player.tscn`; **awaiting local Godot validation**.
- **[Scene_World.tscn](Scene_World.tscn)** — marching-cubes terrain preview; 3×3 static chunks, sky/sun, free-flying debug camera and separated CPU/mesh/collision timing. Mesh resolution and collision radius can be configured in WORLD/Configuration/WorldMaster.tres; **optimization pass awaiting local validation**.
- `Scene_Buildings.tscn`, `Scene_AI.tscn`, `Scene_Game.tscn` — **planned**, not created.

## Running the World preview

Build C# in Godot 4.7.2 .NET, open `SCENE/Scene_World.tscn` and press **F6**. Mouse looks around (inverted vertical by default); WASD/arrow keys fly, Q/E lower/raise, Shift moves faster, Escape releases the cursor and left click recaptures it.

The test's generation/performance settings are in `WORLD/Configuration/WorldMaster.tres`. Change `MeshStep` from 1 to 2 to compare triangle counts at the same physical world size, or adjust `CollisionChunkRadius` (default 0, centre only). Detailed methodology is in [WORLD/README.md](../WORLD/README.md). All terrain code belongs to `WORLD/`; `PLAYER/` is not required for the world test.

Use F6 for either independent test scene. To run either with F5, choose it in **Project Settings → Application → Run → Main Scene**. Keep the existing `project.godot` untouched otherwise.
