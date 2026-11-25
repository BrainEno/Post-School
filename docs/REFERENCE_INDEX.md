# Shared reference index

Post School reuses the approved game-production facts from [Where Is School](https://github.com/BrainEno/where-is-school) while implementing them independently in Unity.

## Imported snapshots

These files were copied from the source repository's `main` branch on 2026-11-25. Each snapshot contains a source link and has relative source links rewritten to GitHub URLs.

- [Product Brief](reference/where-is-school/PRODUCT_BRIEF.md)
- [Narrative Bible](reference/where-is-school/NARRATIVE_BIBLE.md)
- [Concept Direction](reference/where-is-school/CONCEPT_DIRECTION.md)
- [First Vertical Slice](reference/where-is-school/PROTOTYPE_BRIEF.md)
- [Agent Evaluation Protocol](reference/where-is-school/AGENT_EVALUATION.md)

## Referenced but not copied wholesale

- [Art Bible](https://github.com/BrainEno/where-is-school/blob/main/docs/ART_BIBLE.md) — visual reference; Post School may test a different art style.
- [Asset Provenance Ledger](https://github.com/BrainEno/where-is-school/blob/main/docs/ASSET_PROVENANCE.md) — authoritative per-asset rights evidence. Check the exact entry before reusing any source asset.
- [Agent Protocol](https://github.com/BrainEno/where-is-school/blob/main/docs/AGENT_PROTOCOL.md) — source collaboration protocol; this repository has an adapted Unity-specific copy in `docs/AGENT_PROTOCOL.md`.
- [Map Plan](https://github.com/BrainEno/where-is-school/blob/main/docs/MAP_PLAN.md) — reference for shared place names and route intent, not a Unity scene specification.
- [Architecture](https://github.com/BrainEno/where-is-school/blob/main/docs/ARCHITECTURE.md) — Godot implementation contract; consult only for gameplay behavior, never copy engine-specific implementation.

## Update policy

The original repository remains canonical for shared narrative facts. When a source document changes, compare it with the snapshot, update the snapshot deliberately, and record any Unity-specific divergence in this repository. Do not sync Godot-specific implementation instructions blindly.
