# Issue 10 working notes

- Chose a self-contained, untrimmed `win-x64` publish so end users do not install .NET and WPF/EF Core metadata remains intact.
- Added a per-user NSIS installer with stable upgrade identity, Start menu integration, optional desktop shortcut, and non-destructive uninstall behavior.
- Kept installation files and `%LOCALAPPDATA%\Unskip` user data in separate directories.
- Added portable ZIP, SHA-256 checksums, visible application version, assembly metadata, license inclusion, and original logo artwork in SVG, PNG, and multi-resolution ICO formats.
- Added a tag-driven release workflow that validates tags against `main`, repeats deterministic checks, pins the installer compiler by SHA-256, and creates generated release notes.
- Documented unsigned SmartScreen expectations without recommending that users weaken Windows or organizational protections.
- Recorded the future WinGet identity and manifest inputs without publishing a premature manifest.
