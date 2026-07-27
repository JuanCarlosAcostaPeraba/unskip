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

The urgent-attention overlay is currently a local-only preview. It does not listen on a network port, add a firewall rule, register a service, start with Windows, capture the desktop, suppress system shortcuts, move dismissal controls, or reopen after dismissal. Its close button stays in a predictable location; Escape and Alt+F4 remain available; and a bounded timeout always closes it. The preview neither records an acknowledgement nor claims that content was displayed or read.

The protocol-v1 foundation is transport-independent and inactive in the production send path. It defines strict, bounded frames and admission gates for a future receiver but does not bind a socket. The future transport must authenticate before it parses protocol data and must reject any session that is not authenticated, mutually authenticated, encrypted, signed, and associated with a valid remote Windows identity. Payloads contain no sender identity.

Protocol requests use unique IDs, UTC timestamps, short expiries, 128-bit nonces, strict message bounds, a bounded replay window, and per-identity rate limiting. Replay and rate-limit tables have fixed capacities and fail closed rather than evicting live protection state under pressure. An accepted response means only that an authenticated receiver accepted the request for local handling.

The transport decision was amended after confirming that Kerberos SPNs belong to the actual service logon account. A per-user receiver cannot assume the computer account's `HOST/...` SPN. `NegotiateStream` is therefore limited to explicit administrator-provisioned SPN deployments, while mutual TLS is the default deployable direction. Neither mode may accept a session lacking mutual authentication.

For mutual TLS, operating-system chain, hostname, purpose, validity, and revocation validation happens before Unskip authorization. An exact SHA-256 fingerprint is the authoritative identity key; certificate subject text is display-only. The application never generates a CA, installs trust, accepts arbitrary self-signed certificates, disables validation, or persists private keys. [ADR 0001](decisions/0001-authenticated-lan-transport.md) and [certificate deployment](certificate-deployment.md) define these gates.

No production receiver, startup registration, certificate enrollment, trust-store mutation, or firewall rule is approved by those documents.

The mutual-TLS stream boundary is implemented but is not composed into the application. Its public API always uses operating-system certificate validation, online revocation checking, DNS target validation on the client, required client certificates on the server, and effective-protection checks after the handshake. Exact remote fingerprint authorization happens before a protected stream is returned. Failed or timed-out handshakes close the unusable TLS connection.

The infrastructure integration suite uses a private test CA and temporary keys only. Its custom-root validator, disabled revocation lookup, and fixed TLS version are internal test seams and cannot be selected by production callers.

The local SQLite database is not application-encrypted. It relies on the current Windows account and filesystem permissions, stores no credentials or message bodies, and never synchronizes through Unskip. Destination metadata and sanitized diagnostics can still be sensitive.
