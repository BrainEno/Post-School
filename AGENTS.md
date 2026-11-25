# Agent Instructions 鈥?Post School

Read this file, [docs/AGENT_PROTOCOL.md](docs/AGENT_PROTOCOL.md), [docs/PROJECT_RELATIONSHIP.md](docs/PROJECT_RELATIONSHIP.md), and [docs/PORTING_MATRIX.md](docs/PORTING_MATRIX.md) before editing. Read the relevant shared reference documents and the Unity technical baseline for the assigned task.

## Product intent

Post School is a Unity experiment parallel to [Where Is School](https://github.com/BrainEno/where-is-school). Preserve the shared premise and approved narrative constraints while allowing this implementation to test a different visual style and Unity-native architecture. Do not modify the Godot repository from this project.

The primary experience is exploration, environmental observation, and literary dialogue. Combat is not part of the current scope. Do not add enemies, weapons, loot economies, crafting, survival meters, procedural open-world systems, or conventional horror chase loops without explicit owner approval.

## Source of truth

- Shared product scope and premise: `docs/reference/where-is-school/PRODUCT_BRIEF.md`
- Shared narrative facts and character functions: `docs/reference/where-is-school/NARRATIVE_BIBLE.md`
- Shared first-slice intent: `docs/reference/where-is-school/PROTOTYPE_BRIEF.md`
- Shared original concept direction: `docs/reference/where-is-school/CONCEPT_DIRECTION.md`
- Unity-specific scope, scene architecture, and technical choices: this repository's `docs/` documents
- Unity visual experiment: `docs/ART_DIRECTION_EXPERIMENT.md`
- Asset rights and import decisions: `docs/ASSET_PROVENANCE.md`

The original Godot repository is canonical for shared approved game facts. The snapshots in this repository are references; when the source changes, report the discrepancy and update the snapshot deliberately. Do not silently resolve conflicting creative decisions.

## Narrative and author protection 鈥?mandatory

- Preserve uncertainty. Do not confirm that the world is a dream, diagnosis, simulation, conventional parallel universe, or the product of one hidden cause.
- Separate character claims, protagonist recollection, subjective imagination, and events directly observed in the playable scene.
- Q is an ordinary, talkative, overfamiliar classmate鈥攏ot a supernatural guide, deliberate liar, or quest terminal.
- B is absent from the actual first lesson; substitute N takes the class. Do not bring B onstage early to deliver exposition.
- Do not diagnose the protagonist or explain the school's anomalies as a confirmed condition.
- Never copy private manuscript paragraphs, extended descriptions, internal monologues, unpublished endings, or distinctive literary phrasing into source files, prompts, issues, PRs, tests, screenshots, or documentation.
- Do not lightly paraphrase manuscripts paragraph by paragraph. Use only concise approved game-production facts.
- Text the owner edited by hand is author-final. Do not rewrite, shorten, merge, move, restore deleted text, or fix even obvious typos without explicit approval for that specific change.
- New dialogue, narration, choices, and inspection text belong to the owner. Leave approved placeholders blank or reuse already approved lines; register pending text for review.

## Technical baseline

- Unity Editor **6000.3.29f1**, revision **96d2fe17529c**. Do not silently upgrade.
- Unity 3D URP template; C# is the project scripting language unless the owner explicitly changes this decision.
- Use Unity scenes, GameObjects, prefabs, ScriptableObjects, and data assets for composition. Do not build the game as a pure-code renderer.
- Separate world presentation, player control, interaction, dialogue, narrative state, and persistence.
- Keep dialogue, character profiles, item descriptions, and location content out of large gameplay scripts.
- Prefer small, cohesive components and explicit ownership. Avoid duplicate controllers, parallel state stores, or global singleton sprawl.
- Keep the game offline-first. No mandatory online services, analytics, telemetry, paid APIs, or proprietary runtime dependencies.
- Windows PC is first target. Keep input and file paths portable for later Linux/Steam Deck evaluation.

## Search and reuse before implementation 鈥?mandatory

1. Inspect the current branch, working tree, recent changes, and overlapping open PRs before editing.
2. Search existing types, components, events, ScriptableObjects, input actions, tests, and behavior synonyms鈥攏ot only filenames.
3. Reuse the existing owner/API when it fits. If it does not, explain the boundary and reason before adding a parallel implementation.
4. Record the actual search terms, commands, relevant hits, and reuse decision in the PR's **宸叉绱?* section. Never claim a search that was not performed.
5. Keep changes focused. Do not reformat or rewrite unrelated files.

## Art and asset constraints

- Visual style may diverge from the Godot version; preserve readability, authored composition, and narrative intent. See `docs/ART_DIRECTION_EXPERIMENT.md`.
- Graybox assets are encouraged and must be clearly treated as placeholders.
- Do not add generated, downloaded, or third-party assets until their source, license, commercial use, redistribution terms, and modifications are recorded in `docs/ASSET_PROVENANCE.md`.
- Do not copy Godot import sidecars or editor caches. Reimport source assets through Unity and inspect the result.
- Never commit local editor settings, caches, exports, credentials, or personal absolute paths.

## Code quality

- Prefer explicit types for public APIs and non-trivial state.
- Give scenes, prefabs, components, events, input actions, data keys, and story flags stable descriptive names.
- Handle missing or invalid narrative data with actionable errors.
- Do not swallow errors, create fake success paths, or add tests that always pass.
- Do not add dependencies for trivial functionality.

## Testing and evidence

For every code task:
1. State the exact Unity Editor version used.
2. Run a clean project import or batch-mode validation where applicable.
3. Run relevant automated tests; report the exact command and result.
4. Check logs for compilation errors, missing assets, broken references, and runtime errors.
5. Include a concise manual checklist for relevant gameplay paths.
6. Distinguish verified behavior from assumptions and untested behavior. Never claim a test passed unless it was actually run.

Suggested batch-mode test command (adapt the test platform to the actual test suite):

```powershell
& "E:\Programs\Unity\Editors\6000.3.29f1\Editor\Unity.exe" -batchmode -nographics -projectPath "E:\Projects\Post School" -runTests -testPlatform EditMode -testResults "Temp\editmode-results.xml" -logFile "Temp\editmode.log"
```

Do not run tests that write into shipped source data unless the test explicitly uses a temporary copy.

## Git and collaboration

- Use one dedicated feature branch per task. Never push task work directly to `main`.
- Do not overwrite another agent's work, force-push, rewrite history, merge your own PR, or change repository protections.
- Commit messages are English and follow `<type>(<scope>): <imperative summary>`; use lowercase types such as `feat`, `fix`, `refactor`, `test`, `build`, `ci`, `docs`, `perf`, or `chore`.
- Human-facing reports and PR titles/bodies are Simplified Chinese.
- PRs must include purpose, implementation summary, files changed, exact validation commands/results, and known limitations.
- If blocked, report the blocker and the smallest decision needed.

## First prototype priorities

1. Launchable Unity project, input, player movement, and readable camera.
2. One compact classroom with collision and readable navigation.
3. Interactable objects and one NPC.
4. Dialogue presentation with speaker identity, choices, and story flags.
5. A subjective recollection/imagination layer that cannot silently alter objective world state.
6. One clear classroom exit/transition.
7. Basic save/load and usability settings only after the core loop is stable.

Do not build all chapters, complex skill trees, voice acting, cinematic facial animation, or final production art during the first prototype.

## Definition of done

A task is done only when its commits are on the intended branch, acceptance criteria are met, the changed path works in the pinned Unity version, relevant checks are honestly reported, no known regression is concealed, and the PR contains only approved game-production material.


