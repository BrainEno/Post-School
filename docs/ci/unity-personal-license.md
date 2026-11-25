# Unity Personal License for GitHub Actions

The Unity Personal license file on a developer workstation is machine-bound and must not be copied directly into this repository's CI secret. The Unity Editor in GitHub Actions runs on a different machine.

## One-time setup for this repository

1. Open the repository's **Actions** tab.
2. Select **Request Unity Personal License Activation File**.
3. Choose **Run workflow** on the `main` branch.
4. When the run finishes, download the `unity-personal-activation-request` artifact and extract the `.alf` file.
5. Submit that CI-generated `.alf` file through Unity's manual license activation page: https://license.unity3d.com/.
6. Select the Unity Personal license option if offered and download the resulting `.ulf` license file.
7. In **Settings → Secrets and variables → Actions**, replace `UNITY_LICENSE` with the complete contents of that CI-generated `.ulf` file.
8. Re-run **Unity CI** from the Actions tab. Confirm that the EditMode job actually starts Unity and reports test results.

**Important:** Do not use `C:\\ProgramData\\Unity\\Unity_lic.ulf` from the local Windows workstation for this GitHub-hosted runner. Its machine binding does not match the GitHub runner.

If Unity no longer offers Personal manual activation for the generated request file, stop here rather than substituting the local license file. Review the current GameCI activation guidance and Unity's current licensing options before changing secrets.

## Why this workflow exists

GameCI's Personal-license procedure requires the activation request file to be generated on GitHub Actions because the request contains the runner's machine identifiers. A locally generated request file is not interchangeable.

References:
- https://game.ci/docs/3/github/activation/
- https://game.ci/docs/github/activation/
