# Contributing to Unskip

Thank you for helping improve Unskip.

## Workflow

1. Start from an open GitHub issue and read its acceptance criteria.
2. Update local `dev` and create a focused branch such as `feature/3-secure-message-delivery` from it.
3. Keep the change within the issue scope and update affected documentation.
4. Run restore, build, test, and formatting checks documented in the README.
5. Review the diff for secrets, personal data, real network details, and accidental generated files.
6. Open a focused pull request into `dev` that links the issue. Do not close an issue until all acceptance criteria are met.

Feature work and dependency updates must target `dev`. The `main` branch is reserved for reviewed release promotions from `dev`; every new commit merged into `main` starts the automated release workflow. See [docs/releasing.md](docs/releasing.md) before preparing that pull request.

GitHub Actions repeats formatting, Release build, deterministic tests, dependency auditing, and pull-request dependency review on Windows. Native `msg.exe` integration is opt-in and excluded from CI; ordinary tests must never contact a real device.

Release versions and promotion into `main` are maintainer operations. Follow [docs/releasing.md](docs/releasing.md); never reuse a published version, create release tags manually, add a signing claim without a verified certificate, or place local databases and real network data in release inputs.

## Engineering expectations

- Use English for source code and repository documentation.
- Keep business logic out of WPF code-behind.
- Preserve testable boundaries around process execution, storage paths, clocks, and persistence.
- Treat warnings as errors and keep dependencies minimal.
- Never put real workplace names, hosts, IP addresses, credentials, message content, or local databases in the repository.
- Never add telemetry, cloud services, Electron, Node.js, embedded browsers, or sound behavior.
- Never claim that Windows native messaging proves a message was read or acknowledged.

## Security-sensitive changes

Any future process execution must call `msg.exe` directly with `ProcessStartInfo`, `UseShellExecute = false`, and one `ArgumentList` entry per argument. User content must never be executed. See [SECURITY.md](SECURITY.md) and [docs/security.md](docs/security.md).

By participating, you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).
