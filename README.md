# Post School

**Post School** is the Unity experimental implementation of [Where Is School](https://github.com/BrainEno/where-is-school), a surreal narrative exploration game. The two repositories represent parallel engine versions of the same project, not a replacement or a destructive migration.

## Project status

- Unity Editor: **6000.3.29f1** (Unity 6.3 LTS; revision `96d2fe17529c`)
- Template: Unity 3D URP
- Local project: `E:\\Projects\\Post School`
- Current scene: `Assets/Scenes/PostSchool_Prototype.unity`
- Target: Windows PC first; keep the project portable for later Linux/Steam Deck evaluation
- Current stage: clean Unity project foundation and shared production documentation; gameplay systems have not been ported yet

## Relationship to Where Is School

The Godot version remains intact and is the source for approved shared narrative facts. Post School is an experiment: its art direction, presentation, and engine architecture may differ, while the core premise, character functions, narrative boundaries, and comparable prototype scope should remain recognizable.

See [docs/PROJECT_RELATIONSHIP.md](docs/PROJECT_RELATIONSHIP.md) and [docs/PORTING_MATRIX.md](docs/PORTING_MATRIX.md).

## Start

Open the project in Unity Hub or run:

```powershell
unity open "E:\\Projects\\Post School"
```

Use the pinned editor version above. Do not upgrade the project version without an explicit owner decision.

## Documentation

- [AGENTS.md](AGENTS.md) — mandatory agent entry point
- [docs/AGENT_PROTOCOL.md](docs/AGENT_PROTOCOL.md) — collaboration, branch, reporting, and review rules
- [docs/UNITY_TECHNICAL_BASELINE.md](docs/UNITY_TECHNICAL_BASELINE.md) — Unity-specific project conventions
- [docs/ART_DIRECTION_EXPERIMENT.md](docs/ART_DIRECTION_EXPERIMENT.md) — what can differ in the Unity visual experiment
- [docs/PORTING_MATRIX.md](docs/PORTING_MATRIX.md) — what to reuse and what to reimplement
- [docs/reference/where-is-school/](docs/reference/where-is-school/) — dated snapshots of shared source documents; the original repository remains canonical

## Copyright and assets

The project is not granted a blanket open-source license. Original materials remain reserved unless a specific file states otherwise. Third-party assets and dependencies retain their own terms. Do not import an asset merely because it exists in the Godot repository; check its provenance and commercial/redistribution rights first.
