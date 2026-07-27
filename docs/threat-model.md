# Threat model

## Assets

- message text entered by the sender;
- saved aliases and network destinations;
- local send history and diagnostics;
- authenticated Windows sender identity and receiver authorization policy;
- incoming message confidentiality, integrity, freshness, and availability;
- the recipient's foreground desktop and urgent-attention experience;
- the current Windows user's account and network access;
- application binaries, build dependencies, and release integrity.

## Trust boundaries

- user input crossing into validation and process invocation;
- operating-system output returning from `msg.exe`;
- local data crossing into SQLite and diagnostic displays;
- destinations reachable on the local network, which are not inherently trusted;
- untrusted network bytes crossing into an authenticated receiver session and bounded protocol parser;
- authenticated identities crossing authorization, rate-limit, replay, and local UI admission;
- contributor and dependency changes crossing into the public build pipeline.

## Principal threats and controls

| Threat | Required control |
| --- | --- |
| Command injection | No shell; fixed executable; separate validated arguments |
| Unbounded or hung process | Cancellation, timeout, and process cleanup |
| Sensitive data disclosure | Local-only storage, sanitized diagnostics, no body logging by default |
| Misleading delivery claims | Honest statuses; never claim read or acknowledgement |
| Accidental real-device tests | Mocked process boundary; real integration tests explicitly opt-in |
| Repository data leakage | Fictitious examples and ignore rules for databases, logs, and secrets |
| Wrong or spoofed destination | Always show the resolved technical target before sending; validate hostname syntax |
| Unauthorized local database access | Per-user location and Windows filesystem permissions; document absence of application encryption |
| Unsafe support advice | Never recommend blanket firewall, registry, service, or organizational-policy changes |
| Vulnerable dependencies or workflow tampering | Central package versions, vulnerability audit, dependency review, Dependabot, and Actions pinned by commit SHA |
| Sender spoofing or payload impersonation | Derive identity only from a mutually authenticated transport; protocol payload has no sender identity |
| Eavesdropping or message alteration | Require effective encryption and signing after authentication; fail closed on weaker sessions |
| Authentication downgrade | Require authentication, mutual authentication, encryption, and signing instead of trusting handshake success |
| Replay | Unique message ID and 128-bit nonce, both independently cached per authenticated identity in a bounded time window |
| Flooding and resource exhaustion | Authenticate before parsing, bound frame size before allocation, rate-limit per identity, bound identity and replay tables, fail closed at capacity |
| Malformed or downgrade payload | Strict version, JSON member, enum, timestamp, expiry, length, UTF-8, and message-policy validation |
| Unauthorized overlay activation | Admission requires authenticated identity and a future administrator-controlled allow-list before UI dispatch |
| Receiver used as a screen locker | Fixed close action, Escape, Alt+F4, timeout, no system-shortcut suppression, no forced reopening |

## Assumptions and limits

- The sender is authorized to contact the chosen Windows computer and session.
- Windows, the local account, DNS/name resolution, and managed network controls are outside Unskip's trust boundary.
- A compromised Windows account can read or alter that account's Unskip database.
- A zero exit code from `msg.exe` is not evidence of display, reading, identity, or acknowledgement.
- Unskip does not provide end-to-end encryption, recipient authentication, durable queuing, offline delivery, or cloud synchronization.
- The protocol foundation defines recipient authentication requirements but does not yet expose a network receiver or change current delivery.
- A future receiver depends on managed identity, SPN/authorization policy, and an administrator-approved firewall deployment. Workgroup compatibility is not a reason to weaken authentication.
- Local-first operation reduces central data collection but does not make reachable network destinations trusted.

## Security-sensitive changes

Changes to target validation, process execution, protocol framing, authenticated-session checks, replay/rate limits, receiver authorization, diagnostics, persistence, native integration tests, or release packaging require focused tests and review. Real hosts, addresses, identities, credentials, messages, certificates, private keys, databases, and logs must never enter source control.
