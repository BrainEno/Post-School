# Visual and technical direction — Post School

**Status:** Provisional production reference; the owner must approve it against representative in-engine scenes.  
**Engine baseline:** Unity 6.3 LTS, Editor 6000.3.29f1, URP.  
**Target:** Windows PC first; preserve portability for later Linux/Steam Deck evaluation.

## 1. Creative target: coherent postmodern visual language

The intended direction is **postmodern, materially mixed, collage-based surrealism**—not generic low-saturation realism, not a conventional horror look, and not an arbitrary collection of strange props.

The most useful references are the visual experiments in the Godot project:

- [PR #196 — Postmodern collage scene experiments](https://github.com/BrainEno/where-is-school/pull/196)
- [PR #200 — Biology classroom art rework, round two](https://github.com/BrainEno/where-is-school/pull/200)

Treat these as the current visual reference set, not as assets or a demand to copy their implementation. PR #196 explores hand-made collage, impossible spatial logic, expressionist environments, ink-drawn absences, and a transition where a 3D world becomes an illustrated/paper-like stage. PR #200 develops a more composed classroom language: fixed authored camera shots, hard-edged colour fields and ink contours, cast concrete, layered cut-paper pupils, a real projector and a controlled mixed-media composition. The second round deliberately refined the first rather than replacing its approved atmosphere.

**The key correction is that postmodern variety must sit inside a consistent art direction.** The Unity project should preserve the conceptual freedom of collage and media shifts while making every scene feel as if it belongs to the same authored work.

### What should carry across

- **Collage as construction, not decoration:** paper, printed matter, cut edges, layered planes, pasted images, drawn marks, and physical objects should have deliberate relationships and believable material cues.
- **Expressionist space:** scale, perspective, room geometry, repetition, and impossible transitions may be distorted when the distortion carries narrative meaning.
- **Mixed media with intent:** 3D geometry, paper relief, painted surfaces, ink, projected imagery, and illustrated/flat elements can coexist. Each medium needs a clear visual role.
- **Ink and absence:** outlines, annotations, shadows, circles, erased traces, and missing objects can act as narrative marks rather than generic grunge.
- **Everyday institutional objects:** desks, registers, rulers, boards, doors, projectors, concrete, and classroom fittings should retain specificity. Surrealism is stronger when it alters recognizable things.
- **Composed stillness:** authored framing, negative space, and controlled movement should support the literary pacing.

## 2. Coherence system: the rules that unify different media

The following are proposed art-direction controls. They should be recorded as a small visual style guide and tested together, rather than tuned independently by each scene or asset author.

### 2.1 Palette architecture

- Define a project-wide base palette with a limited set of recurring environmental, structural, paper, ink, and accent colours.
- Give each scene a small local palette derived from that shared system; scene variation should come from controlled shifts in temperature, value, and accent—not unrelated colour schemes.
- Reserve the strongest accent colours for narrative emphasis, focal objects, or specific recurring motifs.
- Check colour and value relationships in grayscale as well as colour. Characters, exits, interaction points, and dialogue must remain readable.
- Do not impose one global desaturation filter. A coherent palette can still contain vivid, uncomfortable, or unexpectedly warm passages.

### 2.2 Material taxonomy

Create a small family of recurring material treatments and apply them consistently:

| Material family | Visual identity | Use |
|---|---|---|
| Painted architectural planes | Flat or gently varied colour fields, deliberate hard light/shadow boundaries, restrained ink contour | Walls, doors, large structural surfaces |
| Paper and printed matter | Visible fibre/grain, layered thickness, cut or torn edges, printed/hand-marked variation | Collage, notices, pupils, documents, selected props |
| Ink and drawn marks | Controlled line weight, purposeful imperfections, annotations and shadows | Contours, absence marks, emphasis, selected transitions |
| Concrete and institutional solids | Dense, tactile, restrained aggregate/formwork detail; distinct from paper and painted planes | Floors, platforms, lecterns, structural objects |
| Projected or reproduced image | A clearly framed image plane with its own contrast and resolution logic | Projector screens, windows, pictures, reproduced memories |
| Ordinary 3D objects | Simplified but consistent proportions, silhouette, bevel/detail scale, and surface response | Props and objects that must remain physically legible |

Not every scene needs every family. A scene should use a limited combination and repeat it with discipline.

### 2.3 Geometry, detail, and edge language

- Set consistent rules for bevel size, edge sharpness, silhouette complexity, thickness of paper layers, and the scale of surface detail.
- Distinguish a deliberately cut/pasted edge from a broken mesh, accidental z-fighting, or a low-quality asset.
- Avoid mixing highly realistic scanned textures, smooth generic assets, hand-drawn outlines, and flat paper shapes without a unifying treatment.
- Use repetition, asymmetry, scale shifts, and impossible alignments as authored motifs; do not let them arise from careless placement.
- Establish a hierarchy: large composition first, medium forms second, small texture and marks last.

### 2.4 Character and prop integration

- Characters must belong to the same visual world as the environments, even if their medium differs. Define shared rules for silhouette, proportions, outline, shadow, colour range, and detail density.
- Paper characters should read as intentionally constructed objects, not flat cut-outs accidentally placed in a 3D room.
- Ordinary 3D characters should not become photorealistic by default when the surrounding world is graphic and materially stylized.
- Props need a consistent level of finish and scale. Important narrative objects may receive stronger contrast or a different medium, but that difference must be intentional.

### 2.5 Camera and composition

- Evaluate fixed authored shots as a serious option, informed by PR #200's fixed-camera classroom experiment. Do not assume a constantly following third-person camera is the correct default.
- The camera may change between locations or story beats, but each shot should have a clear composition, stable framing, and a reason to move.
- Preserve player navigation, collision, character visibility, and interaction readability. The camera must not make a visually elegant scene frustrating to play.
- Use negative space and off-centre placement deliberately. Avoid filling every empty area with collage fragments.
- Match framing and visual hierarchy across scenes so that stylistic variation does not feel like separate games.

### 2.6 Lighting and shadow

- Use lighting to reveal the material system: paper layers need readable thickness, concrete needs form and surface response, and painted planes need controlled value separation.
- Hard-edged, expressionist light/shadow can be a recurring device, but it should be authored per scene rather than applied indiscriminately.
- Shadows may become drawn or impossible narrative marks when the scene calls for it; keep ordinary physical shadows coherent elsewhere.
- Do not let lighting wash structural walls into the appearance of open sky or erase silhouettes and navigational cues.

### 2.7 UI and typography

- Dialogue, notebook elements, labels, and interaction prompts should share the same design vocabulary as the world.
- Define typography, line weight, spacing, paper/background treatment, and accent usage centrally.
- Keep reading comfort and hierarchy above decorative novelty. The UI may feel printed or editorial without becoming difficult to read.

## 3. Rendering baseline

### URP first

Keep the current Universal Render Pipeline (URP) template as the baseline. It is the practical starting point for a Windows-first indie project that may later need Linux/Steam Deck portability.

Do not switch to HDRP solely because it is marketed as higher fidelity. Reconsider the pipeline only after a representative scene demonstrates a concrete requirement that URP cannot meet at acceptable quality, performance, and portability.

### Visual tools to evaluate

| Area | Candidate | Intended use | Adoption rule |
|---|---|---|---|
| Materials and stylization | Shader Graph | Shared shader families for painted planes, paper, ink, concrete, and selected media transitions | Build reusable, parameterized treatments; avoid a unique unmaintainable shader per prop |
| Camera | Cinemachine | Authored fixed shots, shot zones, and deliberate transitions where useful | Test fixed-camera and player-follow alternatives; choose based on actual scene readability and comfort |
| Sequencing | Timeline | Specific authored sequences and camera/action timing | Use for sequences that benefit from explicit timeline editing; do not move ordinary gameplay logic into timelines |
| Lighting and atmosphere | URP lights, fog, Volume/post-processing | Reveal material and spatial hierarchy | Prefer authored per-scene lighting over a global effect stack |
| Particles and effects | URP-compatible particle/VFX solution | Selected paper, dust, projection, or transition effects | Check renderer compatibility and target-device cost before adoption |

These are evaluation candidates, not a declaration that every package is installed or required. Before adding a package, verify its exact version, Unity 6000.3 compatibility, license, maintenance status, and effect on builds.

Do not assume HDRP-only features or VFX Graph effects are automatically appropriate for this URP project.

## 4. Camera and spatial presentation

- Build a camera proof-of-concept that compares a fixed authored composition (informed by PR #200) with a controlled follow-camera alternative.
- The initial gameplay baseline may use a readable 2.5D exploration view, but this is not a final camera decision.
- Camera movement should be authored and motivated: zone changes, reveals, transitions, or story beats—not constant movement for its own sake.
- Keep navigation, interaction targeting, character orientation, and dialogue legible through every shot.
- Distorted architecture and impossible space are allowed as art-direction choices, but collision and player affordances must remain consistent with the intended gameplay.

## 5. Lighting, colour, and post-processing

- Establish lighting from each location's composition, practical sources, focal points, and material requirements.
- Use colour grading to maintain the shared palette, not to conceal inconsistent assets or weak lighting.
- Treat bloom, depth of field, vignette, chromatic aberration, film grain, and motion blur as optional. Add each only when it has a clear expressive purpose and does not reduce legibility.
- Prefer material-specific response and authored shadow shapes over a stack of global effects.
- Check URP support, overdraw, transparency sorting, and performance before adopting fog, layered transparency, or full-screen effects.

## 6. Asset and production pipeline

- Build a compact style guide before producing a large asset library: palette, material families, outline rules, detail scale, camera principles, character treatment, and UI typography.
- Create a small representative scene containing architecture, one character, a few props, paper/collage elements, and dialogue UI. Use it as the visual calibration scene for future assets.
- Review new assets in that calibration scene under the same lighting and camera conditions. Do not approve assets in isolation.
- Record all imported, generated, purchased, downloaded, or externally processed assets in [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md) before treating them as production-ready.
- Concept art and AI-generated images are references only; they are not automatically cleared runtime assets.
- Do not copy Godot import sidecars, caches, or engine-generated files. Reimport approved source assets through Unity and validate scale, axes, materials, colliders, and lighting.

## 7. Performance and quality targets

The development PC is not the release performance target. Evaluate representative scenes at realistic output resolutions and quality settings, and profile on hardware closer to the intended player baseline when available.

For each visual experiment, record:

- Unity Editor version and package versions.
- Render pipeline and renderer settings.
- Scene, camera, resolution, quality preset, and test hardware.
- Frame-time or profiler observations, GPU/CPU bottlenecks, memory use, and stutter.
- Visual trade-offs, including whether transparency, outlines, layered paper geometry, or post-processing harmed readability or performance.

Do not claim a performance target has been met without a reproducible measurement.

## 8. Validation gate before adopting the visual direction

A visual experiment is not production direction until it passes all of the following:

1. **Unified:** it follows the shared palette, material taxonomy, geometry/detail scale, and typography rules—or documents a deliberate narrative exception.
2. **Composed:** its camera and scene hierarchy feel authored; collage elements do not become visual clutter.
3. **Playable:** movement, collision, interaction, and exits remain functional.
4. **Readable:** characters, focal objects, routes, interaction targets, and dialogue remain clear.
5. **Narratively appropriate:** surrealism and postmodern media shifts support the writing without defaulting to generic horror.
6. **Performant:** the scene is profiled with recorded settings and acceptable frame pacing on the intended target class.
7. **Rights-cleared:** every production asset has documented provenance and commercial/redistribution status.
8. **Owner-approved:** the owner reviews representative in-engine screenshots or a playable build before the direction becomes a production commitment.

## 9. First Unity visual proof-of-concept

Do not begin by making a large environment or a collection of unrelated showcase effects. Build one **visual calibration scene**—preferably a classroom fragment—containing:

- One coherent architectural composition and a playable character.
- A small, controlled palette.
- At least three material families used together: painted architectural plane, paper/collage, and concrete or another solid material.
- A small number of ink marks or drawn contours.
- One projected/reproduced image or deliberate medium transition.
- A fixed authored camera shot, plus a comparison shot if needed.
- The dialogue/notebook UI.

Compare this scene against the visual evidence in PR #196 and PR #200. The goal is not to reproduce every experimental trick. It is to identify which visual rules make those experiments feel distinctive, then apply those rules consistently.

Change one major variable at a time, capture matching views, and record performance. Do not promote the experiment to the final art direction until the owner confirms that the overall image feels both postmodern and unified.

## 10. Relationship to the source project

The original [Where Is School](https://github.com/BrainEno/where-is-school) Godot repository remains canonical for approved shared narrative facts. Its PR #196 and #200 are visual references for the Unity experiment; their implementation, assets, and exact scene composition are not mandatory porting targets.

This Unity repository may differ in rendering, camera, materials, scene composition, animation, and implementation architecture. Preserve narrative constraints and asset-rights rules in [PROJECT_RELATIONSHIP.md](PROJECT_RELATIONSHIP.md), [PORTING_MATRIX.md](PORTING_MATRIX.md), and [ASSET_PROVENANCE.md](ASSET_PROVENANCE.md).
