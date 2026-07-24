# Security design

Native delivery is isolated in `Unskip.Infrastructure.Windows`. It starts the fixed Windows `msg.exe` executable directly and never invokes a shell.

The delivery implementation:

- validates hostnames with ASCII allow-list rules and IPv4 addresses with strict canonical dotted-decimal rules;
- resolves IPv4 destinations to a hostname and verifies the forward lookup contains the original address before process execution;
- starts `msg.exe` directly with `ProcessStartInfo` and `UseShellExecute = false`;
- places every argument in `ArgumentList` separately;
- never invokes `cmd.exe`, PowerShell, a shell, or string-built commands;
- applies cancellation and a configurable timeout bounded to two minutes;
- captures diagnostics, removes the full message body, strips unsupported control characters, and truncates retained output;
- keeps real-network integration tests opt-in and disabled in CI.

User-supplied message content is data and must never be executed.

The developer attribution opens only the fixed `https://jcap.tech` portfolio URI. External navigation rejects relative, non-HTTPS, and user-information-bearing URI forms, uses the Windows default browser association, and does not invoke a command shell or pass command arguments.

The optional update boundary accepts only the official latest stable GitHub Release, exact HTTPS asset paths for this repository, the expected versioned NSIS filename, bounded file sizes, and the published SHA-256 checksum. Downloads use a temporary extension and become installable only after verification. The installer is verified again immediately before it is started directly with `UseShellExecute = false` and no arguments. Unskip never invokes a command shell to update itself and never performs a silent or mandatory update.

Release `0.1.x` binaries are unsigned. Checksum verification detects corruption or substitution between the published checksum and downloaded installer, but it is not a substitute for Authenticode signing and cannot protect against compromise of the release publisher itself. Organizational policy and SmartScreen decisions remain authoritative.

Unskip does not modify firewall rules, registry entries, Windows services, session rights, or organizational policy. Troubleshooting must prefer target-specific verification and administrator review over blanket security changes.

The local SQLite database is not application-encrypted. It relies on the current Windows account and filesystem permissions, stores no credentials or message bodies, and never synchronizes through Unskip. Destination metadata and sanitized diagnostics can still be sensitive.
