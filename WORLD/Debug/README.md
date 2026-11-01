# World Debug Tools — BW-W4.0

`WorldTestScene.cs` owns F1 generator and benchmark controls; `WorldFlyCamera.cs` owns debug flight and deterministic benchmark travel; `FrameTimeRecorder.cs` is a dependency-free, constant-space histogram. `WorldPreview.cs` remains the sole owner of static terrain generation and mesh/physics timing. The debug tools do **not** generate or unload chunks, and do not participate in eventual gameplay/world saves.

**F6 `SCENE/Scene_World.tscn`:** Press F1 and scroll to BW-W4.0. Start a manual capture (then fly with WASD/arrows), or start a scripted route at 60/180 m/s with an optional instantaneous corner reposition and return. F1 reopens controls; Stop ends recording. Scripted route automatically stops the capture and prints one summary. F1 generator settings and static generation continue to work as before.

The light HUD is refreshed at 2 Hz; capture sampling is constant-time per foreground process callback. Median/P95/P99 use 0.5 ms bins up to 1,000 ms. The stage labels refer to the already-existing generation/collision timers, plus a new removal timer; they are correlations, not GPU or frame-profile attribution. The managed heap number and GC collection counts are **diagnostic hints only**, not full RAM/VRAM measurements. No frame arrays, CSVs, profiling assets or War Machine content are persisted. To compare runs, keep the same scene, grid/seed/mesh-step and graphics settings, and compare cold generation vs warm settled passes.

**Validation status:** Source authored; no Godot/.NET runtime build or performance benchmark was performed by the implementing agent. Do not report FPS improvements before user testing. Future streaming/actor/vegetation counters belong to BW-W4.1–4.5; the present world remains a finite **static** patch.
