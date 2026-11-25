# Agent Instructions — Post School

Before editing, read this file, [docs/AGENT_PROTOCOL.md](docs/AGENT_PROTOCOL.md), [docs/PROJECT_RELATIONSHIP.md](docs/PROJECT_RELATIONSHIP.md), [docs/PORTING_MATRIX.md](docs/PORTING_MATRIX.md), and the relevant Unity technical baseline.

## Product intent

Post School is the Unity experimental implementation parallel to [Where Is School](https://github.com/BrainEno/where-is-school). Preserve the shared premise and approved narrative constraints while allowing a different visual style and Unity-native architecture. Do not modify the Godot repository from this project.

The primary experience is exploration, environmental observation, and literary dialogue. Combat is out of scope. Do not add enemies, weapons, loot economies, crafting, survival meters, procedural open-world systems, or conventional horror chase loops without explicit owner approval.

## Source of truth

- Product scope: `docs/reference/where-is-school/PRODUCT_BRIEF.md`
- Narrative facts: `docs/reference/where-is-school/NARRATIVE_BIBLE.md`
- First slice: `docs/reference/where-is-school/PROTOTYPE_BRIEF.md`
- Concept direction: `docs/reference/where-is-school/CONCEPT_DIRECTION.md`
- Unity technical decisions: this repository's `docs/`
- Visual experiment: `docs/ART_DIRECTION_EXPERIMENT.md`
- Asset rights: `docs/ASSET_PROVENANCE.md`

The original Godot repository is canonical for shared approved facts. Snapshots are references. When source documents change, report discrepancies and update snapshots deliberately; never silently resolve creative conflicts.

## Narrative and author protection — mandatory

- Preserve uncertainty. Do not confirm that the world is a dream, diagnosis, simulation, conventional parallel universe, or the product of one hidden cause.
- Distinguish character claims, protagonist recollection, subjective imagination, and directly observed events.
- Q is an ordinary, talkative, overfamiliar classmate—not a supernatural guide, deliberate liar, or quest terminal.
- B is absent from the actual first lesson; substitute N takes the class. Do not bring B onstage early.
- Do not diagnose the protagonist or explain the school's anomalies as a confirmed condition.
- Never copy private manuscript paragraphs, extended descriptions, internal monologues, unpublished endings, or distinctive literary phrasing into source files, prompts, issues, PRs, tests, screenshots, or docs.
- Do not lightly paraphrase manuscripts paragraph by paragraph. Use only concise approved game-production facts.
- Owner-edited narration, dialogue, choices, and inspection text are author-final. Do not rewrite, shorten, merge, move, restore deleted text, or fix typos without explicit approval for that specific change.
- New dialogue and narration belong to the owner. Leave placeholders or reuse approved lines; register pending text for review.

## Technical baseline

- Unity Editor **6000.3.29f1**, revision **96d2fe17529c**. Do not silently upgrade.
- Unity 3D URP template; C# is the project scripting language unless the owner changes this.
- Use Unity scenes, GameObjects, prefabs, ScriptableObjects, and data assets for composition; do not build a pure-code renderer.
- Separate world presentation, player control, interaction, dialogue, narrative state, and persistence.
- Keep dialogue and location content out of large gameplay scripts.
- Prefer cohesive components and explicit ownership. Avoid duplicate controllers, parallel state stores, or global singleton sprawl.
- Offline-first: no mandatory online services, analytics, telemetry, paid APIs, or proprietary runtime dependencies.
- Windows PC first; preserve portability for later Linux/Steam Deck evaluation.

## Search and reuse before implementation — mandatory

1. Inspect branch, working tree, recent changes, and overlapping PRs.
2. Search existing types, components, events, ScriptableObjects, input actions, tests, and behavior synonyms—not only filenames.
3. Reuse existing owners/APIs when they fit. Explain before introducing parallel implementations.
4. Record actual search commands, hits, and reuse decisions in the PR's **已检索** section.
5. Keep changes focused; do not reformat unrelated files.

## Art and assets

- Visual style may differ from Godot; preserve readability, authored composition, and narrative intent.
- Graybox assets are encouraged and must remain placeholders.
- Before adding generated/downloaded/third-party assets, record source, license, commercial use, redistribution, modifications, and approval in `docs/ASSET_PROVENANCE.md`.
- Do not copy Godot import sidecars or caches. Reimport approved source files through Unity.
- Never commit local editor settings, caches, exports, credentials, or personal absolute paths.

## Code quality and validation

- Use explicit types for public APIs and non-trivial state; stable descriptive names.
- Handle missing narrative data with actionable errors.
- Do not swallow errors, fake success paths, or add always-pass tests.
- Do not add dependencies for trivial functionality.
- For each code task, state the exact Unity version, run import/compile and relevant tests, inspect logs, include manual checks, and distinguish verified behavior from assumptions.

Suggested EditMode test command (run from the project root and replace `Unity.exe` with the installed pinned Editor executable):

```powershell
Unity.exe -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults "Temp\editmode-results.xml" -logFile "Temp\editmode.log"
```

## Git and collaboration

- Use one dedicated feature branch per task; never push task work directly to `main`.
- Do not overwrite another agent's work, force-push, rewrite history, merge your own PR, or change repository protections.
- Commit messages are English: `<type>(<scope>): <imperative summary>`; use `feat`, `fix`, `refactor`, `test`, `build`, `ci`, `docs`, `perf`, or `chore`.
- Human-facing reports and PR titles/bodies are Simplified Chinese.
- PRs include purpose, implementation, files, exact validation, and known limitations.
- If blocked, report the blocker and the smallest decision needed.

## First prototype

1. Launchable project, input, player movement, readable camera.
2. Compact classroom with collision/navigation.
3. Interactable props and one NPC.
4. Dialogue with speaker, choices, and story flags.
5. Subjective recollection isolated from objective world state.
6. One classroom exit/transition.
7. Save/load and usability settings after the core loop stabilizes.

Do not build all chapters, complex skill trees, voice acting, cinematic facial animation, or final production art during the first prototype.

## Definition of done

Acceptance criteria pass; changed paths work in the pinned Unity version; tests and evidence are honest; known regressions are reported; and the PR contains only approved game-production material.
