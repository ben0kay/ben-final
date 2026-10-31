# AI Agent Instructions — Ben Final

**Repo:** `ben0kay/ben-final` · branch `main` unless the owner specifies otherwise. This is the **entry point**; it does not override or replace the referenced documents.

## Before implementation — including long ChatGPT conversations

At the start of each **new substantial task**, or whenever asked to “read AGENTS.md,” **refresh the current repository files**, even if this chat read them earlier. Do this quickly; don't launch broad repository analysis or tests just to refresh instructions:

1. **`CodePreferences.md` — read fully.** Authoritative C# style (`#region`, comments, compact layout), scene-root ownership, architecture, performance, Git and **minimal-testing** rules.
2. **Root `README.md`** — project vision, architecture and implemented vs. planned features.
3. **Root `LESSONS_LEARNED.md` (if present)** — known mistakes, fixes and prevention rules; create it only when a significant reusable lesson warrants it.
4. **Owning feature README/plan, authoring guide, asset README and actual code/scene/resources** relevant to the task. For any terrain, biome, cave, streaming, water, forest or vegetation work **always read `WORLD/WORLD_IMPLEMENTATION_PLAN.md`, `WORLD/WORLD_AUTHORING.md` and `WORLD/README.md`**. The implementation plan contains the latest owner-run benchmark observations, unresolved risks and prioritized Codex tickets; update it when validated findings change. For imported models/materials read `ASSETS/README.md`; for player work read `PLAYER/README.md`. For trees, grass, plants and natural rocks, additionally read a dedicated vegetation authoring guide if later created; Final Machine's visual guide may be consulted as a reference, not assumed to be copied here.

Use `final-machine`, `terra-machine` and `war-machine` only as references when useful; Ben Final is a **custom procedural terrain / marching-cubes experiment**. Do not inherit source-repo branch policies, Terrain3D/Voxel Tools dependencies, assumptions about installed addons or old architecture.

## Working rules

- Implement **only the authorized scope**; preserve user-authored assets, working behaviour, optimizations, unrelated changes and stable scene paths. Follow existing folder ownership, root scene containers, resources and reusable scenes. Avoid gratuitous managers, loops and rewrites.
- Consult past lessons; **fix root causes rather than masking symptoms**. If docs conflict with current code, inspect and flag material discrepancies.
- **Keep validation brief:** focused source review and one basic build/essential check when practical. **No extensive/repeated tests, visual automation or FPS profiling unless requested.** The owner handles most in-Godot visual/performance checks. Clearly distinguish verified from unverified.
- Follow CodePreferences for Git scope/authorization. Prefer **one logical commit** and push when authorized; avoid unrelated commits. Report changes and commit SHA.
- **Maintain only root `LESSONS_LEARNED.md` when needed:** after resolving a significant/reusable mistake, create/update it with symptom, confirmed cause (or uncertainty), fix, prevention and a short verification step **in the same task**. Skip trivial typos, routine tweaks, guesses and duplicates; do not create folder-level lessons files without permission.
- Update a feature README/authoring guide only when enduring design, usage or configuration guidance changes; link to central lessons rather than duplicating lengthy reports.
- Use exact uppercase root folder names `WORLD/`, `PLAYER/`, `ASSETS/`; do not create lowercase duplicates on Windows. Treat the root README's milestone statuses as authoritative. Do not confuse planned test scenes or scripts with implemented ones. Preserve independent `SCENE/Scene_Player.tscn` and future `SCENE/Scene_World.tscn`, Building and AI test environments and integrate reusable scenes instead of hard-coding cross-feature test dependencies.
- Keep handoffs short: changes, commit, checks actually run and any visual/runtime checks left to the owner.

**Reminder:** Reading `AGENTS.md` in an ongoing chat means reread these linked instructions from the repository **before** implementing—not merely acknowledge having read them earlier.
