# Unity Personal License for GitHub Actions

GameCI's current Personal-license setup uses the local Unity Hub-generated `.ulf` file together with Unity account credentials. The old GitHub activation-file request action has been discontinued.

## Required repository secrets

In **Settings → Secrets and variables → Actions**, configure all three secrets:

- `UNITY_LICENSE`: the complete contents of the active Personal `.ulf` license file, not only an XML `value` field.
- `UNITY_EMAIL`: the email address used to sign in to the Unity account.
- `UNITY_PASSWORD`: the Unity account password.

On Windows, the local license file is normally at `C:\ProgramData\Unity\Unity_lic.ulf`. Use Unity Hub → Preferences → Licenses to ensure a valid Personal license has been activated and that this file exists. Never commit the license file or put credentials in workflow YAML.

The Unity CI workflow checks whether all three secrets exist. If any is missing, static project checks still run and the EditMode job explicitly reports **NOT RUN** rather than treating the tests as passed.

If the complete `.ulf` plus credentials still produces a machine-binding error, do not paste a partial XML value or repeatedly reuse the same file. Review the exact Unity activation log and current Unity/GameCI licensing options before making further changes.

References:
- https://game.ci/docs/github/activation/
- https://game.ci/docs/troubleshooting/common-issues/
