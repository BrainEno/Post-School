# Coding-agent evaluation protocol — Post School

Select the primary implementation model based on delivered engineering quality, not confidence, verbosity, or a plausible summary.

## Fair comparison

1. Give every candidate the same task prompt, base commit, Unity version, project settings, acceptance criteria, time, and tool permissions.
2. Use separate branches and clean working copies.
3. Do not let candidates inspect or reuse one another's solutions before submission.
4. Ask each candidate for a focused PR; never allow direct pushes to main.
5. Run the same clean-clone validation and manual acceptance checklist for every PR.
6. Review the actual diff, code, scene/prefab changes, logs, and test evidence—not agent self-report.

Record the agent label, exact model/version if shown, base SHA, branch, PR, changed files, tests, review outcome, and known limitations. If model identity is hidden, record it as unknown.

## Scoring rubric

| Category | Weight | Evidence |
|---|---:|---|
| Functional correctness | 25 | Reproducible acceptance criteria, edge cases, no broken flows |
| Architecture and integration | 20 | Clear ownership, minimal coupling, fits project boundaries |
| Code quality and maintainability | 15 | Readable C#, scenes/prefabs/data assets, error handling |
| Tests and validation | 15 | Meaningful tests, clean import, honest runtime evidence |
| Narrative/gameplay fidelity | 15 | Supports exploration/dialogue and preserves narrative boundaries |
| Scope discipline and documentation | 10 | Focused diff, useful docs, clear limitations |

Score each category from 0 to 5 and multiply by its weight. Track blocking defects separately; a high total cannot excuse a serious regression or dishonest test claim.

## Review checklist

- Does the project open in the pinned Unity Editor?
- Are scenes, prefabs, materials, and data assets referenced correctly?
- Is state ownership explicit and testable?
- Are narrative data and control logic separated?
- Do interaction and dialogue have clear ownership?
- Are missing data and failure paths handled?
- Are tests meaningful rather than constant-only or always-pass?
- Are controls, camera, collision, typography, and long text usable?
- Are generated/third-party assets recorded and cleared?
- Does the PR stay within its assigned scope?

## Primary-model decision

Choose the candidate with the strongest correctness, integration, maintainability, and verified execution. Do not select the one with the most lines or largest feature list. Keep the decision provisional until the model succeeds on at least two distinct tasks, such as one gameplay task and one dialogue/data-integrity task.
