# Unity CI on the Windows development machine

The repository uses a self-hosted Windows runner for Unity EditMode tests. This avoids copying a workstation-bound Unity Personal license into a GitHub-hosted Linux container.

## Runner requirements

- Windows x64 machine with the repository's Unity Editor version, **6000.3.29f1**
- Unity Hub signed in and Unity Personal activated for the Windows user that runs the runner
- GitHub Actions runner registered to this repository with the custom label `post-school-unity`
- Runner started and online before a main-branch push or manual workflow dispatch

The workflow discovers the Editor through this registry key:

`HKLM\SOFTWARE\Unity Technologies\Installer\Unity 6000.3.29f1`

and its `Location x64` value. It then runs Unity's built-in command-line Test Runner with `-batchmode -nographics -runTests -testPlatform EditMode`.

## Security boundary for this public repository

This repository is public. **The self-hosted Unity job is intentionally skipped on pull requests**, including fork PRs, because untrusted pull-request code must not execute on a personal computer. PRs still run the GitHub-hosted static checks. The Unity test job runs only on trusted pushes to `main` or an owner-triggered `workflow_dispatch`.

Keep the runner assigned only to this repository, use the dedicated `post-school-unity` label, and do not add that label to workflows that execute pull-request code.

## License troubleshooting

The Windows machine's Unity Personal license is activated locally. Do not copy its `Unity_lic.ulf` into GitHub Actions secrets for the hosted Linux runner; that caused the earlier machine-binding mismatch.

If the runner reports an activation error, sign in to Unity Hub under the same Windows account that launches the runner, verify the Personal license in **Preferences → Licenses**, then restart the runner. Never commit a license file or Unity account credentials to the repository.

## Local verification

The EditMode suite can be run from PowerShell:

```powershell
& 'E:\Programs\Unity\Editors\6000.3.29f1\Editor\Unity.exe' `
  -batchmode -nographics -projectPath 'E:\Projects\Post School' `
  -runTests -testPlatform EditMode `
  -testResults 'E:\temp\PostSchool-EditMode-results.xml' `
  -logFile 'E:\temp\PostSchool-EditMode-test.log'
```
