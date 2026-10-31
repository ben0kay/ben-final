# Ben Final — Lessons Learned

Document only significant, reusable mistakes. Preserve an explicit difference between a suspected cause and a locally confirmed cause.

## BW-W1 — Black terrain in first marching-cubes preview (October 30, 2026)

- **Symptom:** `SCENE/Scene_World.tscn` generated nine of nine chunks and about 21,000 triangles, but the terrain appeared completely black rather than grass-green/rock-grey.
- **Investigation:** The original mesher emitted triangles in the reverse order from the classic 256-case lookup table, while the `ArrayMesh` expects a consistent Godot front-face winding. The terrain shader used `cull_disabled`, letting back-facing triangles render. The scene also configured `ambient_light_source = 2` (ambient colour) without an explicit ambient colour; it was not sampling sky ambient illumination.
- **Fix applied:** Emit table triangles in their original order, enable `cull_back` for this single-sided terrain surface, and use `ambient_light_source = 3` (sky). Preserve the indexed mesh, world-space normal gradients, collision and worker queue.
- **Verification required:** Build C# in Godot, run `SCENE/Scene_World.tscn` with F6 and confirm grass/rock coloration, visible top faces, sun response, and no holes between the nine chunks. Also check collider walking and terrain underside once the player is integrated.
- **Prevention:** For each marching-cubes implementation, test a single flat signed-density case with the engine's front-face convention and an upward-looking normal. Inspect ambient-source settings explicitly; a visible sky background doesn't automatically mean the ambient light configuration is correct. Do not hide winding problems with double-sided rendering.
- **Status:** **Suspected root causes; corrected in source, not yet verified visually in Godot.**

## BW-W2.5 — F1 requested size reverted to 27×27 (October 31, 2026)

- **Symptom:** The owner typed `50` in the F1 chunks-per-side field, pressed Regenerate, but the world still showed the 27×27 startup grid.
- **Suspected cause (not confirmed):** The prior Godot `SpinBox` may have displayed uncommitted typed text while the C# callback read its older `.Value` of 27 when the button received focus. The world grid mathematics already supported 50×50.
- **Fix authored:** Replace chunk count and seed SpinBoxes with LineEdit text fields, parse/validate the visible text at button press, show the interpreted NxN count in the panel and log the exact request. Don't silently clamp malformed input to a default. Add explicit confirmation for >50×50 static loads.
- **Prevention:** UI text, committed control values and generation parameters must be cross-checked at the action boundary. Assert test cases including **typing without Enter**, 9→81 chunks, 50→2,500, and invalid text rejected, plus button focus changes.
- **Status:** Source change staged; **awaiting owner Godot runtime confirmation**. Do not call the suspected SpinBox commit behavior the proven cause until verified.
