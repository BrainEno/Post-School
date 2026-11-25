# Project relationship — Where Is School and Post School

## Decision

- **Where Is School** is the existing Godot implementation.
- **Post School** is the Unity experimental implementation.
- They are parallel versions of one game concept, not a forced migration and not a shared working tree.
- The Godot repository remains untouched by Unity work and continues to hold the canonical approved shared narrative facts unless the owner explicitly changes that arrangement.

## What should remain comparable

- Core premise: an adult protagonist occupies the role of a student again, without a confirmed explanation.
- Narrative uncertainty and the distinctions between claims, recollection, imagination, and observed events.
- Character functions and opening facts, including Q's ordinary classmate role and B's absence/N's substitution.
- First-slice scope: classroom, movement, interaction, dialogue choices, story state, subjective recollection, and an exit.
- Quality bar: real playable behavior, reproducible tests, honest evidence, and reviewable PRs.

## What may differ

- Rendering pipeline, camera implementation, scene composition, materials, shaders, lighting, character proportions, animation, UI layout, and Unity-specific architecture.
- The Unity version may explore a distinct art style. Do not treat visual parity as a requirement.
- The two versions may differ in implementation details as long as the owner-approved narrative facts and comparison scope remain clear.

## Experiment discipline

For meaningful comparisons, keep the prototype task, acceptance criteria, target platform, and test conditions comparable. Record where the implementations deliberately diverge. Do not compare one version's graybox to the other's finished art as if that were an engine-only result.

## Asset policy

Do not assume assets in the Godot repository are cleared for Unity reuse. Recheck provenance, license, commercial use, redistribution, and conversion rights. Unity imports must be performed from approved source assets; do not copy Godot-generated import data or caches.
