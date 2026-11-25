# Art direction experiment — Post School

## Intent

The intended direction is **postmodern, materially mixed, collage-based surrealism**, informed by the visual experiments in the original Godot project:

- [PR #196 — Postmodern collage scene experiments](https://github.com/BrainEno/where-is-school/pull/196)
- [PR #200 — Biology classroom art rework, round two](https://github.com/BrainEno/where-is-school/pull/200)

These PRs are references for visual principles, not instructions to copy every asset or effect. PR #196 explores hand-made collage, impossible spatial logic, expressionist environments, ink-drawn absences, and a 3D-to-illustrated medium shift. PR #200 refines the biology classroom with fixed authored camera shots, hard-edged colour fields and ink contours, concrete, layered cut-paper pupils, a projector, and a more controlled composition.

The central art-direction problem to solve is **how to retain postmodern variety without making the game look like a collection of unrelated experiments**. Unity should develop a consistent visual grammar that can accommodate different media and scene-specific distortions.

This is not generic low-saturation realism, conventional horror, or random surreal decoration.

## Principles worth preserving

- **Collage as construction:** use layered paper, printed matter, torn/cut edges, drawn marks, and physical objects with intentional material relationships.
- **Expressionist space:** alter scale, perspective, repetition, and room geometry when the distortion has narrative meaning.
- **Mixed media with hierarchy:** 3D geometry, paper relief, painted planes, ink, projected images, and illustrated elements may coexist, but each needs a clear role.
- **Ink and absence:** contours, annotations, impossible shadows, circles, erased traces, and missing objects can become narrative marks.
- **Recognizable institutional specificity:** desks, registers, rulers, boards, doors, projectors, concrete, and classroom fittings should remain particular objects, not generic environment dressing.
- **Composed stillness:** fixed framing, negative space, deliberate asymmetry, and controlled transitions can carry the literary pacing.
- **Material coherence:** paper should read as paper, concrete as concrete, painted surfaces as painted surfaces; their coexistence should feel authored rather than accidental.

## Unity's shared visual grammar

Build a compact style guide and a calibration scene before producing large quantities of art.

1. **Palette:** one shared base palette with controlled scene-level variations; reserve accents for narrative emphasis. Do not use a global desaturation filter as a substitute for palette design.
2. **Material families:** define repeatable treatments for painted architectural planes, paper/printed matter, ink/drawn marks, concrete/solid surfaces, projected imagery, and ordinary 3D objects.
3. **Geometry and edge rules:** standardize bevel size, paper thickness, outline weight, silhouette/detail density, and surface-detail scale.
4. **Characters and props:** ensure they belong to the same world through consistent silhouette, proportions, outline, shadow, colour range, and finish—even when they use a different medium.
5. **Camera and composition:** evaluate fixed authored shots as in PR #200 against a controlled follow-camera alternative. Each shot needs a reason and must preserve navigation and interaction readability.
6. **Lighting:** use deliberate scene-specific light and shadow to reveal material; impossible or drawn shadows are intentional exceptions, not accidental inconsistencies.
7. **UI and typography:** dialogue, notebook, labels, and prompts should share the same editorial/printed vocabulary while remaining comfortable to read.

See [VISUAL_TECHNICAL_DIRECTION.md](VISUAL_TECHNICAL_DIRECTION.md) for the technical plan, rendering baseline, camera experiments, and acceptance gate.

## What the Unity version may experiment with

- Reusable Shader Graph families for painted planes, paper, ink, concrete, and selected medium transitions.
- Fixed authored camera compositions, controlled shot changes, and carefully chosen cinematic movement.
- Scene-specific expressionist lighting and hard-edged shadows.
- Layered paper geometry, printed/cut textures, projected imagery, and transitions between 3D and illustrated presentation.
- Different character proportions, animation language, or dialogue composition, provided the shared visual grammar remains intact.

These are experiments, not a requirement to use every effect in every scene.

## Guardrails

- Do not lock final art direction before the owner has reviewed a representative Unity calibration scene in motion.
- Do not interpret “postmodern” as permission for arbitrary collage clutter, unrelated asset packs, random palettes, or inconsistent rendering styles.
- Do not make every surface dirty, rusty, wet, or covered in graffiti by default.
- Avoid generic horror signifiers and effects that obscure navigation, characters, or dialogue.
- Do not pursue photorealism, expensive effects, or high asset density as proxies for quality.
- Do not treat AI-generated or downloaded concept art as cleared production assets.
- Record asset provenance and obtain owner review before a visual experiment becomes production direction.
- Keep the game playable and performant on the intended Windows PC target; do not optimize exclusively for the development GPU.

## Reference

The original [Where Is School Concept Direction](https://github.com/BrainEno/where-is-school/blob/main/docs/CONCEPT_DIRECTION.md) and [Art Bible](https://github.com/BrainEno/where-is-school/blob/main/docs/ART_BIBLE.md) remain narrative/design references. PR #196 and PR #200 are the specific visual experiment references for this Unity art-direction pass. The owner decides which Unity-specific deviations to retain.
