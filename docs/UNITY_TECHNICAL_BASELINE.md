# Unity technical baseline

## Pinned environment

- Editor: **6000.3.29f1** (Unity 6.3 LTS)
- Editor revision: **96d2fe17529c**
- Template: **3D URP**
- Local working copy: owner's E-drive checkout; exact path is machine-specific and intentionally omitted from shared docs
- Local-only Unity project; no Unity Cloud project is required
- Windows PC first; preserve portability for later Linux/Steam Deck evaluation

Do not change the Editor version, render pipeline, package baseline, or project architecture without documenting the reason and obtaining owner approval when the change affects the experiment.

## Current foundation

- `Assets/Scenes/PostSchool_Prototype.unity` — initial prototype scene (currently a template/graybox scene, not the playable game)
- `Assets/Scripts/Core/` — small shared runtime foundations
- `Assets/Scripts/Interaction/` — interaction ownership
- `Assets/Scripts/Dialogue/` — dialogue presentation and choice flow
- `Assets/Scripts/Narrative/` — story state and conditions/effects
- `Assets/Data/` — data assets and ScriptableObjects
- `Assets/Art/` — approved art sources and Unity-imported assets
- `docs/` — production decisions and validation notes

## Architectural direction (provisional)

- Use Unity scenes and prefabs for composition; keep gameplay behavior in focused C# components.
- Use ScriptableObjects or simple data assets for character profiles, dialogue nodes, inspectable props, and location definitions where this improves authoring and testability.
- Keep dialogue content out of large MonoBehaviour scripts.
- Define one owner for player movement, interaction targeting, dialogue state, story flags, and persistence.
- Avoid a service locator or static singleton for every subsystem. Start with a small scene/bootstrap composition and introduce persistent services only when a real cross-scene requirement exists.
- Prefer built-in Unity features and packages already in the template. Add dependencies only for a demonstrated need and record exact version and license.
- Keep game logic testable without requiring a rendered frame where practical.

## Rendering and presentation direction

- URP remains the baseline renderer for the current experiment. Do not switch to HDRP merely to pursue a higher-quality label; evaluate actual visual needs, target hardware, portability, and measured performance first.
- The intended exploration is a deliberately authored, atmospheric 3D presentation with a readable 2.5D exploration camera—not a default photorealistic or generic horror treatment.
- Evaluate Shader Graph for a small number of coherent material treatments (for example, painterly surface response, controlled outlines, or selective stylization). A shader is an experiment until it passes scene readability, lighting, performance, and art-direction review.
- Evaluate Cinemachine for authored camera framing and controlled transitions, and Timeline for specific scripted sequences where they materially improve pacing. These are candidate Unity packages, not blanket requirements; verify package availability/version against the pinned Editor before adoption.
- Use URP-compatible lighting, fog, color grading, and post-processing selectively. Preserve readable navigation, interaction targets, character silhouettes, and dialogue.
- Do not add HDRP-only features or assume VFX Graph effects are automatically suitable for the URP target. Check renderer compatibility and target-device cost before committing to an effect.
- The detailed, provisional visual and technical plan is in [VISUAL_TECHNICAL_DIRECTION.md](VISUAL_TECHNICAL_DIRECTION.md).

## Input and camera

The template includes the Unity Input System. Use it as the default input path; do not introduce a second input implementation without a clear reason. The camera is expected to support a readable 2.5D exploration view, but its final projection, framing, and movement are intentionally not fixed yet.

## Asset import

- Import from approved source assets only.
- Record source, rights, commercial-use and redistribution status, modifications, and approval in `docs/ASSET_PROVENANCE.md`.
- Verify scale, axes, materials, texture sizes, collider setup, and runtime readability after import.
- Never copy Godot import sidecars, `.godot/`, `Library/`, `Temp/`, or generated caches into source control.

## Validation

Use the exact Editor version pinned above. For automated tests, prefer Unity Test Framework EditMode tests for data/logic and PlayMode tests for scene behavior. Record actual commands, logs, and limitations; do not claim a batch-mode check substitutes for hands-on camera, collision, readability, or interaction playtesting.
