# Agent collaboration and reporting protocol — Post School

This protocol is mandatory for every coding agent, model, Arena session, and task in this repository. It supplements [AGENTS.md](../AGENTS.md).

## 1. Language

- Human-facing progress reports, blockers, PR titles/bodies, review replies, and final handoffs must be in Simplified Chinese.
- Commit messages must be in English and follow `<type>(<scope>): <imperative summary>`.
- Keep code identifiers, file paths, API names, command lines, test output, and official product names unchanged where precision requires it.
- Never infer or invent a model identity from a Git author, branch name, or generic agent metadata. If the platform does not show it, report that it cannot be verified.

## 2. Branch and PR

- One dedicated branch per task and per PR; use lowercase English kebab-case, e.g. `agent/<short-task-name>`.
- Target `main` unless the owner explicitly says otherwise.
- PR titles use a Chinese category prefix: `[功能]`、`[修复]`、`[重构]`、`[测试]`、`[构建]`、`[文档]`、`[资源]`.
- Do not push directly to main, force-push, rewrite history, merge your own PR, or alter repository protection settings.
- Do not stack on an unmerged branch unless the owner explicitly authorizes it.

## 3. Commit format

```text
<type>(<scope>): <imperative summary>
```

Allowed types: `feat`, `fix`, `refactor`, `test`, `build`, `ci`, `docs`, `perf`, `chore`.
Scope is required, lowercase kebab-case, and names the subsystem. English imperative summary; no trailing period; keep subject at or below 72 characters where practical. Do not add unverified co-author or model attribution.

## 4. Before editing

- Inspect the branch and working tree; do not overwrite existing changes.
- Search code, scenes, prefabs, components, ScriptableObjects, input actions, tests, and behavior synonyms.
- Record the real search commands and relevant hits in the PR's **已检索** section.
- Reuse existing ownership and APIs. Explain before introducing a parallel controller, state store, or service.
- Check source-of-truth docs and current project settings before proposing a new architecture.

## 5. Author text and private material

- Owner-edited narrative text is final. Do not rewrite, shorten, reorder, restore deleted text, or "polish" it without explicit approval.
- New authored dialogue or narration must remain a placeholder or be listed as pending owner text.
- Never place private manuscripts or distinctive manuscript phrasing in code, documentation, prompts, issues, PRs, tests, or screenshots.
- Do not send complete manuscripts to external coding agents.

## 6. Assets and dependencies

- Every imported or generated asset needs a provenance record before it is used in a build.
- Record creator/source, exact source URL or supplied-file origin, license/terms, commercial-use status, redistribution status, modifications, and approval.
- Unknown rights means **not cleared for public or store release**.
- Pin exact dependency versions and record license, maintenance status, and adoption reason. No paid or mandatory online dependencies without owner approval.

## 7. Validation and reporting

Every task report must include:
- exact Unity Editor version;
- changed files and implementation summary;
- commands actually run and their results;
- relevant logs or test evidence;
- manual acceptance checklist;
- known limitations and unverified behavior;
- remaining decisions, if any.

Never claim an import, test, build, screenshot, or manual playtest passed unless it was actually performed. Do not create fake success paths or tests that always pass.

## 8. PR template requirement

Every PR description must contain:
- purpose and scope;
- implementation summary;
- files changed;
- **已检索**: actual search commands, hits, and reuse decision;
- exact validation commands/results;
- manual checks and evidence;
- known limitations and pending owner decisions.

If blocked, stop before making a destructive or scope-changing assumption. Report the blocker and the smallest decision required.
