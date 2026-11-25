# Visual and technical direction — Post School

**Status:** Provisional production reference; requires owner review in a playable Unity scene.  
**Engine baseline:** Unity 6.3 LTS, Editor 6000.3.29f1, URP.  
**Target:** Windows PC first; preserve portability for later Linux/Steam Deck evaluation.

## 1. Creative target

Post School is an experimental Unity interpretation of Where Is School, not a requirement to reproduce the Godot project's exact rendering or assets.

The working visual hypothesis is a **literary, atmospheric, surreal 3D exploration game**. Ordinary school and residential environments should remain recognizable and navigable; unease should emerge through spatial composition, scale, silence, framing, lighting, and carefully chosen contradictions.

Quality should come from coherent art direction and authored presentation—not from maximizing polygon count, photorealism, effects, or asset quantity.

Desired qualities to test:

- Restrained, intentional color relationships rather than a universal desaturated filter.
- Strong composition, negative space, readable silhouettes, and a small number of meaningful environmental details.
- Material surfaces with a controlled visual character; painterly treatment is an option to prototype, not a locked requirement.
- Cinematic atmosphere without sacrificing walkable-space, interaction, and dialogue readability.
- Scene transitions and camera language that support literary pacing and uncertainty.

## 2. Rendering baseline

### URP first

Keep the current Universal Render Pipeline (URP) template as the baseline. It is the practical starting point for a Windows-first indie project that may later need Linux/Steam Deck portability.

Do not switch to HDRP solely because it is marketed as higher fidelity. Reconsider the pipeline only after a representative scene demonstrates a concrete requirement that URP cannot meet at acceptable quality, performance, and portability.

### Visual tools to evaluate

| Area | Candidate | Intended use | Adoption rule |
|---|---|---|---|
| Materials and stylization | Shader Graph | Prototype a small, consistent set of painterly/stylized material responses | Keep only after readability, lighting interaction, and performance review |
| Camera | Cinemachine | Controlled framing, follow behavior, and deliberate transitions | Verify package availability/version in the pinned Editor; avoid unnecessary camera abstraction |
| Sequencing | Timeline | Specific authored sequences and camera/action timing | Use for sequences that benefit from explicit timeline editing; do not move ordinary gameplay logic into timelines |
| Lighting and atmosphere | URP lights, fog, Volume/post-processing | Establish mood, depth, and visual hierarchy | Prefer authored per-scene lighting over a global effect stack |
| Particles and effects | URP-compatible particle/VFX solution | Dust, rain, subtle environmental motion, or selected narrative effects | Check renderer compatibility and target-device cost before adoption |

These are evaluation candidates, not a declaration that every package is installed or required. Before adding a package, verify its exact version, Unity 6000.3 compatibility, license, maintenance status, and effect on builds.

Do not assume HDRP-only features or VFX Graph effects are automatically appropriate for this URP project.

## 3. Camera and spatial presentation

- Retain a readable 2.5D exploration camera as the initial gameplay baseline.
- The final projection, framing, and degree of camera movement remain open design questions.
- Test fixed or semi-fixed authored compositions against player-controlled exploration before committing to a global camera model.
- Camera movement must not obscure exits, walkable surfaces, interaction targets, character orientation, or dialogue.
- Use scene-specific framing where it supports a deliberate change in mood; avoid camera changes that feel arbitrary.
- Keep navigation and interaction rules stable even when visual perspective or composition changes.

## 4. Lighting, color, and post-processing

- Establish lighting from the needs of each location: time, practical sources, depth, focal points, and intended emotional temperature.
- Use color grading to unify a scene, not to conceal inconsistent asset colors or weak lighting.
- Treat bloom, depth of field, vignette, chromatic aberration, film grain, and motion blur as optional effects. Add each only when it has a clear expressive purpose and does not reduce legibility.
- Use fog and volumetric-looking depth cues only after checking URP support, overdraw, transparency sorting, and performance on target hardware.
- Preserve readable character silhouettes and important environmental affordances in the darkest or most stylized scenes.

## 5. Art and asset pipeline

- Build the first representative environment from graybox geometry and a small, curated set of cleared assets.
- Prefer a consistent scale, texel density, material response, lighting vocabulary, and level of detail over a large collection of unrelated asset packs.
- Record all imported, generated, purchased, downloaded, or externally processed assets in [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md) before treating them as production-ready.
- A concept image or AI-generated image is visual reference only; it is not automatically a cleared runtime asset.
- Do not copy Godot import sidecars, caches, or engine-generated files. Reimport approved source assets through Unity and validate scale, axes, materials, colliders, and lighting.
- Keep the source asset and its license/redistribution evidence traceable.

## 6. Performance and quality targets

The development PC is not the release performance target. Evaluate representative scenes at realistic output resolutions and quality settings, and profile on hardware closer to the intended player baseline when available.

For each visual experiment, record:

- Unity Editor version and package versions.
- Render pipeline and renderer settings.
- Scene, camera, resolution, quality preset, and test hardware.
- Frame-time or profiler observations, GPU/CPU bottlenecks, memory use, and any stutter.
- Visual trade-offs and whether readability or interaction was harmed.

Do not claim a performance target has been met without a reproducible measurement. Avoid expensive full-screen effects, excessive transparent layers, unnecessary real-time lights/shadows, and oversized textures unless their value is demonstrated.

## 7. Validation gate before adopting a visual direction

A visual experiment is not production direction until it passes all of the following:

1. **Playable:** the graybox route, movement, collision, interaction, and exit remain functional.
2. **Readable:** players can identify the route, interactables, characters, and dialogue without fighting the art treatment.
3. **Coherent:** environment, character, UI, lighting, and effects belong to one intentional visual language.
4. **Narratively appropriate:** the scene supports ambiguity and atmosphere without explaining the world's mystery or imposing a generic horror genre.
5. **Performant:** the scene is profiled with recorded settings and acceptable frame pacing on the intended target class.
6. **Rights-cleared:** every production asset has documented provenance and commercial/redistribution status.
7. **Owner-approved:** representative in-engine screenshots or a playable build are reviewed before the experiment becomes a production commitment.

## 8. First visual proof-of-concept

After the first classroom graybox is playable, create one small visual test scene or controlled classroom pass:

- Keep the same geometry, camera route, and interaction points across comparisons.
- Compare a neutral URP baseline with one restrained stylized/painterly material treatment.
- Test one authored lighting setup and a restrained color grade.
- If useful, compare fixed/semi-fixed framing with the current exploration camera.
- Capture matching screenshots and record frame-time/profiler observations.
- Change one major variable at a time so the result is attributable.
- Ask the owner to choose what to retain; do not silently promote an experiment to the final art direction.

Do not build a large environment, final character shader, complex cinematic pipeline, or expensive effect stack before this proof-of-concept establishes a useful direction.

## 9. Relationship to the source project

The original [Where Is School](https://github.com/BrainEno/where-is-school) Godot repository remains canonical for approved shared narrative facts. This Unity repository may diverge in rendering, camera, materials, scene composition, animation, and implementation architecture.

Preserve the narrative constraints and asset-rights rules in [PROJECT_RELATIONSHIP.md](PROJECT_RELATIONSHIP.md), [PORTING_MATRIX.md](PORTING_MATRIX.md), and [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md). Do not treat visual similarity as a requirement or copy assets without rights review.
