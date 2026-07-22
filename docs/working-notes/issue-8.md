# Issue 8 working notes

- Added a least-privilege Windows CI workflow for restore, formatting, Release build, deterministic tests, vulnerability auditing, and test-result artifacts.
- Added pull-request dependency review with immutable action SHAs.
- Added weekly grouped Dependabot updates for NuGet and GitHub Actions.
- Added an explicit `NativeIntegration` test boundary that requires two opt-in environment variables and remains excluded from CI.
- Expanded the pull request template with native-test and security checks.
- Added the README status badge only after the first GitHub workflow completed successfully.
