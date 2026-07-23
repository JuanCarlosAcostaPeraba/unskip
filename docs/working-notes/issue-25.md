# Issue 25 working notes

- The first update experience is user-initiated; startup and messaging never depend on Internet access.
- Discovery uses [GitHub's official latest stable Release endpoint](https://docs.github.com/rest/releases/releases#get-the-latest-release) without a user token.
- Only exact HTTPS asset paths under this repository are accepted.
- The expected installer name is derived from the validated semantic version.
- Installer size is bounded and must match release metadata.
- `SHA256SUMS.txt` must contain the exact installer filename and the downloaded bytes must match its SHA-256.
- The installer is verified again immediately before process launch.
- Installer execution uses `UseShellExecute = false`, no arguments, and no shell expansion.
- The application requests shutdown only after the installer process starts successfully.
- Silent updates and background polling remain out of scope until release signing is available.
- [Microsoft documents](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation) that unsigned releases may show SmartScreen warnings and cannot inherit publisher reputation.
