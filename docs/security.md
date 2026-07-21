# Security design

Native delivery is isolated in `Unskip.Infrastructure.Windows`. It starts the fixed Windows `msg.exe` executable directly and never invokes a shell.

The delivery implementation:

- validates hostnames with ASCII allow-list rules and rejects IPv4 with a documented explanation;
- start `msg.exe` directly with `ProcessStartInfo` and `UseShellExecute = false`;
- place every argument in `ArgumentList` separately;
- never invoke `cmd.exe`, PowerShell, a shell, or string-built commands;
- applies cancellation and a configurable timeout bounded to two minutes;
- captures diagnostics, removes the full message body, strips unsupported control characters, and truncates retained output;
- keep real-network integration tests opt-in and disabled in CI.

User-supplied message content is data and must never be executed.
