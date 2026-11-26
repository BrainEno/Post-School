# Core Gameplay — Post School

> Status: owner-approved direction; detailed skill names and numerical tuning remain provisional.
> This document defines Post School's independent Unity gameplay experiment. It is not a requirement to reproduce Where Is School's implementation.

## 1. Product identity

Post School is a text-forward, literary psychological RPG built around exploration, observation, character dialogue, subjective interpretation, and player-defined character tendencies.

It is the Unity experimental counterpart to Where Is School, not a strict remake. It may diverge substantially in presentation, structure, and mechanics while preserving owner-approved shared narrative facts and boundaries.

Combat is not part of the core loop. Do not add combat, enemies, loot economies, crafting, survival meters, or conventional horror chase systems without explicit owner approval.

## 2. Core dialogue experience

Dialogue is a primary gameplay space, not a small subtitle layer attached to movement.

- When a meaningful conversation begins, pause or greatly reduce world simulation and prioritize the exchange.
- Use a tall, continuously readable dialogue/narration column, initially explored on the right side of the screen.
- The text stream may include spoken lines, environmental description, facial expression, posture, gestures, pauses, and the protagonist's reactions or internal voices.
- The default conversation mode is focused one-to-one dialogue. Other characters should not casually interrupt or interleave lines; interruptions must be authored and purposeful.
- Preserve accumulated dialogue context and provide comfortable reading, scrolling, and text-speed/accessibility controls as the UI develops.
- Choices may be gated, altered, revealed, or contextualized by attributes and skills. A check should not automatically reduce to a pass/fail door; failure may reveal information, change tone, or create a different narrative consequence.
- Do not generate or rewrite author-final dialogue and narration. Use approved text or placeholders pending owner writing/review.

The intended reference is the *kind* of sustained, scene-prioritizing literary dialogue found in Disco Elysium: borrow the readable narrative rhythm and character-focused presentation, not its exact text, UI, characters, or distinctive expression.

## 3. Five core attributes

The character system has exactly five primary attributes:

1. **体魄 (Physique)** — bodily capacity, endurance, pain, physical presence and impulses.
2. **精神 (Psyche)** — will, emotional regulation, empathy, social influence and psychological resistance.
3. **智性 (Intellect)** — reasoning, learned knowledge, language, memory and analysis.
4. **感知 (Perception)** — observation, sensory acuity, composure, reaction and environmental sensitivity.
5. **想象 (Imagination)** — symbolic association, dreamlike thinking, creative projection, inner imagery and alternative interpretations.

These five attributes form the main character-build axis. Skill names and their exact effects are a design proposal and may be tuned after prototype testing.

### Suggested skill directions

The following six skill directions per attribute are a starting vocabulary, not a locked final skill tree.

| Attribute | Suggested skills |
|---|---|
| 体魄 | 耐力、忍痛、身体力量、身体意识、行动协调、冲动本能 |
| 精神 | 意志、共情、情绪感应、心理防御、社交影响、内心回声 |
| 智性 | 逻辑、知识、记忆、语言表达、抽象联想、事件推演 |
| 感知 | 观察、听觉、直觉、反应、镇定、环境敏感 |
| 想象 | 象征联想、梦境思维、形象投射、荒诞联结、可能性构造、虚构感知 |

Knowledge is a skill direction under Intellect rather than a separate primary attribute, keeping knowledge and reasoning related but distinct. Imagination is its own primary attribute and must not be treated as merely a synonym for Intellect or Perception.

Skills may eventually appear as internal voices, instincts, or interpretive impulses. Such voices can be insightful, biased, contradictory, or mistaken; they must not serve as an omniscient explanation of the world's mysteries.

## 4. Initial character creation

The initial character build uses five attributes and seven free allocation points.

- Each of the five attributes begins at **1**.
- The player receives **7 additional points** to allocate freely.
- The player chooses the allocation according to their preferred character tendency; points are not randomly assigned.
- The proposed initial attribute cap is **5**. Treat this as a provisional balance constraint until tested.
- The opening character creation should communicate the narrative feel of each attribute, not only its numeric effect.

This produces a starting total of 12 attribute points. Example:

| Attribute | Example value |
|---|---:|
| 体魄 | 3 |
| 精神 | 4 |
| 智性 | 2 |
| 感知 | 2 |
| 想象 | 1 |
| **Total** | **12** |

This example is illustrative only, not a prescribed build.

## 5. Protagonist concept and visual direction

The protagonist should become more androgynous and culturally non-specific than the Where Is School counterpart.

- Avoid defining the protagonist as specifically Asian, African, American, or another fixed ethnicity/nationality through default design.
- Favor an ambiguous, non-binary-coded silhouette and an expressive, psychologically legible presence. Do not rely on ethnic stereotypes or generic “global” costume cues.
- The design should feel intentionally authored and strange, not like a conventional realistic human model.
- Prioritize expressionism, postmodern collage logic, deliberate material contradiction, distortion, and bold artistic decisions. The project should embrace postmodernism consistently rather than use it as occasional decoration.
- A handmade clay / stop-motion-like indie-game aesthetic is a valid visual reference direction: tactile surfaces, sculpted forms, imperfect edges, unusual proportions, and a slightly unstable physical presence. Do not copy a specific game's assets or signature character design.
- **3D is preferred** because the project is a Unity visual experiment and spatial staging matters. Use a custom sculpted/clay-like 3D model, non-photoreal materials, silhouette and lighting experiments first.
- If 3D cannot communicate the protagonist's expressionist design at an acceptable quality, a deliberate 2D or hybrid 2D/3D presentation is an approved alternative. Do not force weak 3D merely for consistency.
- Compare approaches with the same brief and capture evidence; document why the selected approach communicates the character best.

The art direction is experimental. The first prototype should establish a coherent, high-quality visual language rather than attempt a large amount of final art.

## 6. First playable loop

The initial gameplay loop should be small and testable:

1. Explore a compact classroom and observe objects or spatial details.
2. Approach and interact with one character.
3. Enter focused dialogue, with the scene paused or visually quieted.
4. Read authored narration and dialogue; choose responses.
5. Encounter an attribute/skill-informed option or check.
6. See a meaningful result or alternate interpretation.
7. Return to exploration and reach the first exit/transition.

Keep dialogue content in data assets separate from large gameplay scripts. Separate dialogue presentation, dialogue state, character attributes, check resolution, interaction, and world simulation.

## 7. Prototype boundaries

For the first slice, prioritize:
- a launchable Unity scene and stable player interaction;
- five-attribute character creation with seven free points;
- one focused dialogue sequence with narration and choices;
- at least one attribute-informed option/check;
- pause/resume behavior and readable text presentation;
- a clear testable result and exit.

Do not implement all 30 proposed skills, a complete skill tree, multiple chapters, voice acting, cinematic facial animation, or final production art in the first task.

## 8. Validation and ownership

- Use the pinned Unity Editor version documented in the repository.
- Verify actual behavior in Unity; distinguish automated tests from manual visual checks.
- Keep narrative facts, claims, memories, imagination, and observed events distinct.
- Do not diagnose the protagonist or resolve the world's ambiguity without owner approval.
- Follow `AGENTS.md`, the agent protocol, asset provenance rules, and Unity technical baseline.
