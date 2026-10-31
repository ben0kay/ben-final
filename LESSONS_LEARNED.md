# Ben Final — Lessons Learned

Document only significant, reusable mistakes. Preserve an explicit difference between a suspected cause and a locally confirmed cause.

## BW-W1 — Black terrain in first marching-cubes preview (October 30, 2026)

- **Symptom:** `SCENE/Scene_World.tscn` generated nine of nine chunks and about 21,000 triangles, but the terrain appeared completely black rather than grass-green/rock-grey.
- **Investigation:** The original mesher emitted triangles in the reverse order from the classic 256-case lookup table, while the `ArrayMesh` expects a consistent Godot front-face winding. The terrain shader used `cull_disabled`, letting back-facing triangles render. The scene also configured `ambient_light_source = 2` (ambient colour) without an explicit ambient colour; it was not sampling sky ambient illumination.
- **Fix applied:** Emit table triangles in their original order, enable `cull_back` for this single-sided terrain surface, and use `ambient_light_source = 3` (sky). Preserve the indexed mesh, world-space normal gradients, collision and worker queue.
- **Verification required:** Build C# in Godot, run `SCENE/Scene_World.tscn` with F6 and confirm grass/rock coloration, visible top faces, sun response, and no holes between the nine chunks. Also check collider walking and terrain underside once the player is integrated.
- **Prevention:** For each marching-cubes implementation, test a single flat signed-density case with the engine's front-face convention and an upward-looking normal. Inspect ambient-source settings explicitly; a visible sky background doesn't automatically mean the ambient light configuration is correct. Do not hide winding problems with double-sided rendering.
- **Status:** **Suspected root causes; corrected in source, not yet verified visually in Godot.**
