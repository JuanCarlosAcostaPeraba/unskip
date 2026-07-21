# Security design

The current foundation performs no process execution or network delivery.

Future delivery code must:

- validate hostnames and supported IP inputs with allow-list rules;
- start `msg.exe` directly with `ProcessStartInfo` and `UseShellExecute = false`;
- place every argument in `ArgumentList` separately;
- never invoke `cmd.exe`, PowerShell, a shell, or string-built commands;
- apply cancellation and a bounded timeout;
- capture and sanitize diagnostics without logging full message bodies by default;
- keep real-network integration tests opt-in and disabled in CI.

User-supplied message content is data and must never be executed.
