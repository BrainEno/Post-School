# Character art

Use graybox placeholders until the owner approves a visual direction and asset provenance. Character identity should remain readable at gameplay distance.

## What is in use

The protagonist and Q are **procedurally generated skinned characters** built at runtime by `Assets/Scripts/Character/`:

- `HumanoidRig.cs` — 19-bone skeleton plus the two proportion tables (`Protagonist`, `Classmate`).
- `SkinnedFigureFactory.cs` / `SkinnedMeshBuilder.cs` — one mesh per figure, four material surfaces, up to four bone influences per vertex.
- `HumanoidPose.cs` / `HumanoidClip.cs` — hand-authored keyframes, no imported animation.
- `HumanoidAnimator.cs` — samples the clips on unscaled time.

No model, rig, texture or motion file is imported. Both figures share the skeleton, so a clip written once plays on both; the difference between them lives entirely in their proportions and in which clips they run.

## What is legacy

`ProceduralStudentModelFactory.cs` is a primitive-composed placeholder factory kept only for the visual-calibration scene, where it serves as the before-picture next to the skinned figures. Nothing in gameplay should call it.

## Status

Both are original designs informed by owner-provided references only. They are not final or production-cleared character art, and no reference image, model, name or exact design was imported or copied.
