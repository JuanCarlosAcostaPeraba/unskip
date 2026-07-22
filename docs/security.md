# Security design

Native delivery is isolated in `Unskip.Infrastructure.Windows`. It starts the fixed Windows `msg.exe` executable directly and never invokes a shell.

The delivery implementation:

- validates hostnames with ASCII allow-list rules and rejects IPv4 with a documented explanation;
- starts `msg.exe` directly with `ProcessStartInfo` and `UseShellExecute = false`;
- places every argument in `ArgumentList` separately;
- never invokes `cmd.exe`, PowerShell, a shell, or string-built commands;
- applies cancellation and a configurable timeout bounded to two minutes;
- captures diagnostics, removes the full message body, strips unsupported control characters, and truncates retained output;
- keeps real-network integration tests opt-in and disabled in CI.

User-supplied message content is data and must never be executed.

Unskip does not modify firewall rules, registry entries, Windows services, session rights, or organizational policy. Troubleshooting must prefer target-specific verification and administrator review over blanket security changes.

The local SQLite database is not application-encrypted. It relies on the current Windows account and filesystem permissions, stores no credentials or message bodies, and never synchronizes through Unskip. Destination metadata and sanitized diagnostics can still be sensitive.
