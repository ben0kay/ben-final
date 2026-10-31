# SCENE — Independent Test Worlds

Test scenes instance reusable feature scenes; gameplay and generation code remain in their owning folders.

- **[Scene_Player.tscn](Scene_Player.tscn)** — graybox FPS room with an instanced `res://PLAYER/Player.tscn`; **awaiting local Godot validation**.
- **[Scene_World.tscn](Scene_World.tscn)** — marching-cubes terrain preview; **27×27 static chunks spanning 1,728×1,728 m by default**, F1 numeric chunk count / seed / seven geography sliders / detail with regeneration, sky/sun and free-fly camera. Mesh resolution, chunk radius and collision range are in WORLD/Configuration/WorldMaster.tres; **larger preview awaiting local validation**.
- `Scene_Buildings.tscn`, `Scene_AI.tscn`, `Scene_Game.tscn` — **planned**, not created.

### Scene root categories

The world test follows the shared scene ownership convention from [CodePreferences](../CodePreferences.md). The root `Scene_World` has the reusable `World`, `WorldActorsRoot` for world props/actors, `EnvironmentRoot` for sunlight/sky, `DebugRoot` for the free camera, and `DebugUIRoot` for the HUD. Category-only nodes are plain containers with no scripts or idle processing; gameplay-only roots are **not** created until needed.

```text
Scene_World
├── World                     (instance of WORLD/World.tscn)
│   └── TerrainRoot           (generated chunks)
├── WorldActorsRoot
│   └── DebugPropsRoot
│       └── ScalePine_14m     (temporary scale landmark)
├── EnvironmentRoot
│   ├── WorldEnvironment
│   └── Sun
├── DebugRoot
│   └── DebugCamera
└── DebugUIRoot               (CanvasLayer)
    ├── Controls
    ├── Statistics
    └── WorldSettingsPanel (hidden until F1)
```

### Scale reference

The composed meadow/foothills/mountains test still instances **one low-poly 14 m tall pine-shaped landmark** from `WORLD/Debug/WorldScaleTree.tscn` at x=8, z=90, visible ahead of the starting camera. `WorldScaleTree.cs` samples the **same seed and density-height function** as the terrain and positions the trunk base on the surface when the scene loads. It has **no collider or per-frame updates**, and is not part of the eventual procedural forest system. In the Godot scene tree, expand **WorldActorsRoot → DebugPropsRoot**. Select `ScalePine_14m` to move, hide (eye icon / Inspector Visible), or **delete the single scene instance** when you no longer need the reference. No terrain script needs editing. If removed permanently, the unused tree scene and its C# script may also be deleted, but only after confirming nothing else instances them. Its exported `WorldNodePath` is relative to this container layout and points to `../../../World`.

## Running the World preview

Build C# in Godot 4.7.2 .NET, open `SCENE/Scene_World.tscn` and press **F6**. Press **F1** for the editor panel, enter a chunks-per-side number (e.g. 9 → 9×9), adjust seed / seven geography sliders / mesh detail and click **Regenerate world**. Press F1 again to fly. Mouse looks around (inverted vertical by default); WASD/arrow keys fly, Q/E lower/raise, Shift moves faster (now 180 m/s), Escape releases the cursor and left click recaptures it.

The test's generation/performance settings are in `WORLD/Configuration/WorldMaster.tres`. Change `MeshStep` from 1 to 2 to compare triangle counts at the same physical world size, or adjust `CollisionChunkRadius` (default 0, centre only). Detailed methodology is in [WORLD/README.md](../WORLD/README.md). All terrain code belongs to `WORLD/`; `PLAYER/` is not required for the world test.

Use F6 for either independent test scene. To run either with F5, choose it in **Project Settings → Application → Run → Main Scene**. Keep the existing `project.godot` untouched otherwise.
