# Art direction experiment — Post School

## Intent

Post School is allowed to test a visual identity distinct from Where Is School. The source Art Bible and Concept Direction remain valuable references for narrative atmosphere and composition, but this repository does not require pixel-for-pixel or asset-level visual parity.

The current hypothesis is a **literary, atmospheric, surreal 3D adventure**: recognizable everyday school and residential spaces first, with carefully controlled spatial unease, authored composition, and a restrained visual language. This is a direction to test, not a final art lock.

## Principles worth preserving

- Make people psychologically specific through silhouette, posture, gesture, proportion, and selective color.
- Treat ordinary school and residential spaces as material, recognizable places before introducing spatial contradiction.
- Use negative space, waiting, and a few isolated objects as expressive tools.
- Keep the playable route, walkable surfaces, interaction targets, and immediate choices legible.
- Prefer a small number of authored spaces over generic scale or dense clutter.
- Distinguish dialogue, memory, imagination, and directly observed events through presentation—not by confusing the player.
- Build atmosphere through composition, lighting, material response, sound, and pacing together; do not rely on a post-processing preset to create the whole identity.

## What the Unity version may experiment with

- Painterly or selectively stylized 3D materials, while retaining readable forms and surfaces.
- Cinematic framing, controlled camera movement, and scene-specific projection; a readable 2.5D exploration camera remains the initial gameplay baseline.
- A restrained material language, authored lighting, fog, color grading, and post-processing.
- Unity-native Shader Graph experiments, with separate evaluation for environment and character materials where needed.
- Carefully staged transitions or short cinematic beats using Unity-native camera and sequencing tools when justified.
- A different character proportion, animation language, or dialogue composition.

See [VISUAL_TECHNICAL_DIRECTION.md](VISUAL_TECHNICAL_DIRECTION.md) for the provisional rendering, camera, lighting, and validation plan.

## Guardrails

- Do not lock final art direction before the graybox is playable and the owner has reviewed representative in-engine scenes.
- Do not make every surface dirty, rusty, wet, or covered in graffiti by default.
- Avoid generic horror signifiers, jump-scare framing, or dense surreal collages that obscure navigation.
- Avoid pursuing photorealism, expensive effects, or high asset density as proxies for quality.
- Do not treat AI-generated or downloaded concept art as cleared production assets.
- Record asset provenance and obtain owner review before a visual experiment becomes production direction.
- Keep the game playable and performant on the intended Windows PC target; do not optimize exclusively for the development GPU.

## Reference

The shared source concepts are in [the original Concept Direction](https://github.com/BrainEno/where-is-school/blob/main/docs/CONCEPT_DIRECTION.md) and [Art Bible](https://github.com/BrainEno/where-is-school/blob/main/docs/ART_BIBLE.md). They inform the experiment; the owner decides which Unity-specific deviations to retain.
