# PLAYER — First-Person Player Systems

**Status:** Initial controller, camera, capsule, configuration and input/movement/stance helpers authored; awaiting local C# build and play validation.

`PLAYER/` is the sole owner of the reusable first-person player scene and its gameplay code.

## Intended ownership

- `Player.tscn` + `PlayerController.cs`: reusable `CharacterBody3D`, capsule collision, and first-person camera rig.
- `Input/PlayerInput.cs`: sole owner of physical arrow keys, Shift sprint, RMB jump, right-Ctrl crouch toggle, inverted mouse look, Escape release and LMB recapture.
- `Movement/PlayerMovement.cs`: walking, acceleration, braking, sprint, jump, gravity and `MoveAndSlide()`.
- `Movement/PlayerStance.cs`: animated crouch capsule and eye height, plus clearance testing when standing.
- `Configuration/PlayerSettings.cs` + `PlayerMaster.tres`: tunable movement, stance and mouse settings.
- Weapons, lean, inventory, interaction, stamina and survival systems remain **planned**.
- Other folders (inventory, health, stamina, audio) should be added **only when implemented**, with coherent ownership.

## Isolation and integration

The authored `SCENE/Scene_Player.tscn` instances `PLAYER/Player.tscn` into a simple graybox room, including a low ceiling to exercise crouching. Build C#, then open the scene and press F6. **Player must not depend on `WORLD/World.tscn`, marching cubes, terrain-specific node paths or a particular map.**

Later `SCENE/Scene_Game.tscn` can instance the same Player scene into WORLD and provide spawn placement, collision/world queries, interaction interfaces and shared signals as necessary. Borrow selected working mechanics from [War Machine](https://github.com/ben0kay/war-machine) only after reviewing dependencies and receiving authorization; this guide does not copy any code.

Read [root README](../README.md), [AGENTS](../AGENTS.md) and [CodePreferences](../CodePreferences.md) before implementation.
