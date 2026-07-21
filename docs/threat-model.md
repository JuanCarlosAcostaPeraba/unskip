# Initial threat model

## Assets

- message text entered by the sender;
- saved aliases and network destinations;
- local send history and diagnostics;
- the current Windows user's account and network access.

## Trust boundaries

- user input crossing into validation and process invocation;
- operating-system output returning from `msg.exe`;
- local data crossing into SQLite and diagnostic displays;
- destinations reachable on the local network, which are not inherently trusted.

## Principal threats and controls

| Threat | Required control |
| --- | --- |
| Command injection | No shell; fixed executable; separate validated arguments |
| Unbounded or hung process | Cancellation, timeout, and process cleanup |
| Sensitive data disclosure | Local-only storage, sanitized diagnostics, no body logging by default |
| Misleading delivery claims | Honest statuses; never claim read or acknowledgement |
| Accidental real-device tests | Mocked process boundary; real integration tests explicitly opt-in |
| Repository data leakage | Fictitious examples and ignore rules for databases, logs, and secrets |

This model will be expanded as delivery and persistence are implemented.
