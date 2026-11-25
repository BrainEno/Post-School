# Agent collaboration and reporting protocol — Post School

Mandatory for every coding agent, model, Arena session, and task. This supplements [AGENTS.md](../AGENTS.md).

## Language

- Human-facing progress reports, blockers, PR titles/bodies, review replies, and final handoffs must be in Simplified Chinese.
- Commit messages must be in English and follow `<type>(<scope>): <imperative summary>`.
- Keep code identifiers, file paths, API names, commands, test output, and official product names unchanged where precision requires it.
- Never infer model identity from a Git author or branch. If the platform does not show it, say it cannot be verified.

## Branch and PR

- One dedicated branch per task/PR; lowercase English kebab-case, e.g. `agent/<short-task-name>`.
- Target `main` unless the owner explicitly says otherwise.
- PR titles use Chinese category prefixes: `[功能]`、`[修复]`、`[重构]`、`[测试]`、`[构建]`、`[文档]`、`[资源]`.
- Do not push directly to main, force-push, rewrite history, merge your own PR, or alter repository protections.
- Do not stack on an unmerged branch unless the owner explicitly authorizes it.

## Commit format

```text
<type>(<scope>): <imperative summary>
```

Allowed types: `feat`, `fix`, `refactor`, `test`, `build`, `ci`, `docs`, `perf`, `chore`. Scope is required, lowercase kebab-case, and names the subsystem. Use an English imperative summary, no trailing period, preferably <=72 characters. No unverified co-author/model attribution.

## Before editing

- Inspect branch, working tree, recent changes, and PR overlap.
- Search code, scenes, prefabs, components, ScriptableObjects, input actions, tests, and behavior synonyms.
- Record actual search commands, hits, and reuse decisions in **已检索**.
- Reuse current owners/APIs; explain before adding parallel controllers, state stores, or services.
- Check source-of-truth docs and project settings before proposing architecture.

## Author text and private material

- Owner-edited narrative is final. Do not rewrite, shorten, reorder, restore deleted text, or polish without explicit approval.
- New dialogue/narration remains a placeholder or is marked pending owner text.
- Never place private manuscripts or distinctive manuscript phrasing in code, docs, prompts, issues, PRs, tests, or screenshots.
- Do not send complete manuscripts to external agents.

## Assets and dependencies

- Every imported/generated asset needs a provenance record before use in a build.
- Record creator/source, exact URL or supplied-file origin, license, commercial-use and redistribution status, modifications, and owner approval.
- Unknown rights mean **not cleared for public/store release**.
- Pin dependency versions and record license, maintenance, and reason. No paid or mandatory online dependencies without approval.

## Validation and report

Report exact Unity version, changed files, commands/results, logs/tests, manual checks, known limitations, unverified behavior, and owner decisions. Never claim an import/test/build/playtest passed unless it was actually performed. No fake success paths or always-pass tests.

## PR description must include

- purpose and scope;
- implementation summary and changed files;
- **已检索**: actual searches, hits, and reuse decisions;
- exact validation commands/results;
- manual checks/evidence;
- known limitations and pending owner decisions.

If blocked, stop before destructive or scope-changing assumptions. Report the blocker and the smallest decision required.
