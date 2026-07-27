# ADR 0001: authenticated LAN transport

- Status: amended by issue #61; protocol foundation accepted, mTLS is the default deployable direction, production listener not approved
- Date: 2026-07-27
- Issue: [#59](https://github.com/JuanCarlosAcostaPeraba/unskip/issues/59)

## Context

Unskip needs code running in the recipient's interactive Windows session before it can replace the constrained `msg.exe` window with its own urgent-attention overlay. Any reachable receiver would accept sensitive message text and could trigger a foreground UI, so transport authentication is a prerequisite rather than an optional enhancement.

The application remains local-first and has no central identity provider of its own. It must not invent passwords, distribute shared secrets, trust a sender name inside JSON, or silently weaken protection for workgroup compatibility.

## Original decision

The first design preferred Windows integrated authentication through .NET `NegotiateStream`. A connection was usable only when the completed authenticated stream reported all of the following:

- authentication succeeded;
- mutual authentication succeeded;
- data is encrypted;
- data is signed;
- the remote Windows identity is present and passes the configured authorization policy.

The application would request `EncryptAndSign` and then verify the effective stream properties. A successful handshake alone remained insufficient. A negotiation that fell back to a mechanism without mutual authentication was rejected.

The authenticated transport identity is authoritative. Protocol v1 intentionally contains no sender-name or sender-identity field.

## Amendment: per-user SPN constraint

Implementation review confirmed that a Kerberos SPN belongs to the account under which a service instance runs. Unskip intentionally runs as the interactive user, not as `LocalSystem` or a privileged Windows service, so it cannot assume ownership of the computer account's `HOST/...` SPN.

`NegotiateStream` remains an optional enterprise mode only when an administrator:

- registers a unique SPN on the receiver's actual logon account;
- distributes the exact target SPN to authorized clients;
- prevents duplicate SPNs;
- proves that the resulting stream is mutually authenticated, encrypted, and signed;
- accepts the lifecycle implications when the receiving user or run-as account changes.

Unskip must reject NTLM or any other negotiated result that lacks mutual authentication. It must not register or modify SPNs itself.

The default deployable direction for the per-user receiver is therefore mutual TLS with administrator-issued certificates and operating-system trust validation.

## Mutual TLS deployment decision

Mutual TLS moves identity lifecycle into certificate operations. A safe deployment requires:

- a trusted issuing CA and explicit client/server certificate purposes;
- private-key protection and per-device enrollment;
- hostname validation and certificate-chain validation;
- expiry monitoring, renewal, revocation, and rollback;
- administrator-controlled trust distribution.

Unskip will not generate a private CA, trust self-signed peer certificates automatically, install trust roots, disable chain or hostname validation, or store exportable shared client keys. Certificate provisioning and revocation remain administrator responsibilities.

After the operating system validates the chain and certificate purpose, Unskip authorizes the peer by an exact canonical SHA-256 certificate fingerprint. The fingerprint becomes the authoritative identity key used by allow-listing, replay protection, and rate limiting. Certificate subjects and friendly names are display-only and cannot authorize a sender.

Exact fingerprint authorization is intentionally simple and fail-closed, but certificate renewal usually changes the fingerprint. Administrators must deploy an overlap allow-list containing the old and new fingerprints, deploy the renewed certificates, verify connectivity, and then remove the old fingerprint. [Certificate deployment](../certificate-deployment.md) records the required process.

## Protocol v1 boundary

Protocol data crosses only an already authenticated and protected stream. Each frame has a four-byte unsigned-size-compatible, big-endian length prefix followed by one UTF-8 JSON value. Payloads are limited to 16 KiB before allocation.

A request contains exactly:

- protocol version;
- unique message ID;
- UTC send timestamp;
- UTC expiry;
- 128-bit cryptographic nonce encoded as Base64;
- explicit message kind;
- message text bounded to the existing 1,024-character policy.

Unknown properties, unsupported enum values, unsupported versions, non-UTC timestamps, expired requests, excessive lifetimes, malformed UTF-8 JSON, truncated frames, invalid lengths, and trailing JSON content are rejected.

An accepted response means only **accepted for local handling**. Responses may also report rejected, rate limited, or unsupported version. They never claim display, reading, understanding, or acknowledgement.

## Replay and flooding controls

Admission is keyed by the authenticated identity. Both message IDs and nonces are independently unique within a bounded five-minute replay window. The cache has a fixed capacity and fails closed when full.

Each authenticated identity is limited to ten admission attempts per minute. The identity table is bounded and also fails closed at capacity. These in-process controls complement, but do not replace, administrator allow-lists and network policy.

## Consequences

- Per-user Kerberos deployment requires an administrator-provisioned SPN tied to the actual run-as account.
- Workgroup machines are not silently downgraded to unauthenticated messaging.
- Message payloads cannot impersonate another Windows identity.
- mTLS deployments require administrator-owned certificate issuance, trust, renewal, and revocation.
- Protocol parsing and admission can be tested without opening sockets.
- This decision adds no listener, firewall rule, startup entry, service, discovery, remote overlay activation, or production send-path change.

## References

- [Microsoft: `NegotiateStream`](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.negotiatestream?view=net-10.0)
- [Microsoft: `AuthenticateAsClient` and effective protection checks](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.negotiatestream.authenticateasclient?view=net-10.0)
- [Microsoft: `SslStream` client authentication](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.sslstream.authenticateasclient?view=net-10.0)
- [Microsoft: Windows Firewall rules](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)
- [Microsoft: how a service registers its SPNs](https://learn.microsoft.com/en-us/windows/win32/ad/how-a-service-registers-its-spns)
- [Microsoft: configure an SPN](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/how-to-configure-spn)
