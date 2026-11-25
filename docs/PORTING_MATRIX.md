# Porting matrix — reusable facts vs Unity implementation

This document prevents a naive engine-to-engine file copy.

| Source material / system | Reuse in Post School | Unity treatment |
|---|---|---|
| Product premise, design pillars, scope guardrails | Yes | Keep as shared game facts; reference source snapshots |
| Narrative Bible and character functions | Yes | Keep approved facts and uncertainty boundaries |
| Owner-approved dialogue/narration | Only when explicitly approved for game use | Preserve exact text and stable content IDs |
| First vertical-slice acceptance criteria | Yes, with engine validation rewritten | Revalidate in Unity; do not claim Godot tests apply |
| Agent collaboration, review, and author-text protections | Yes | Use this repository's adapted protocol |
| Art Bible / concept direction | Reference, not a mandatory visual clone | Unity may test different visual treatment; owner decides |
| Location names, route logic, concise scene beats | Reuse as game-production facts | Rebuild scenes and transitions in Unity |
| GDScript | No direct reuse | Reimplement in C# or approved Unity-native code |
| Godot `.tscn`, `.tres`, node trees, autoloads | No direct reuse | Unity scenes, prefabs, ScriptableObjects, and scoped services |
| Godot navigation / `NavigationRegion3D` | Behavior contract only | Evaluate Unity AI Navigation/NavMesh workflow |
| Godot input maps and signals | Behavior contract only | Use Unity Input System and C# events/interfaces |
| Godot editor addons/tools | Do not copy blindly | Reimplement only if the prototype proves the need |
| Godot `.import` sidecars, `.godot/`, caches, exports | Never | Unity reimports approved source files |
| GLB/Blender source assets | Only after rights review | Record provenance, import settings, scale, axes, materials, colliders |
| Fonts, portraits, music, audio, generated assets | Only after rights and attribution review | Record source and release clearance in asset ledger |

## Porting order

1. Establish a launchable Unity project and pinned Editor.
2. Write a small scene ownership and input contract.
3. Graybox the classroom and player movement.
4. Add contextual interaction and one inspectable prop.
5. Add dialogue data, choices, and story flags.
6. Add a subjective recollection presentation isolated from objective world state.
7. Add one transition and a repeatable playtest.
8. Only then evaluate reusable tools, portrait workflow, and asset migration.

Do not port every Godot system before proving the first Unity slice.
