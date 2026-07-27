# ADR 0001: authenticated LAN transport

- Status: accepted for protocol foundation; production listener not approved
- Date: 2026-07-27
- Issue: [#59](https://github.com/JuanCarlosAcostaPeraba/unskip/issues/59)

## Context

Unskip needs code running in the recipient's interactive Windows session before it can replace the constrained `msg.exe` window with its own urgent-attention overlay. Any reachable receiver would accept sensitive message text and could trigger a foreground UI, so transport authentication is a prerequisite rather than an optional enhancement.

The application remains local-first and has no central identity provider of its own. It must not invent passwords, distribute shared secrets, trust a sender name inside JSON, or silently weaken protection for workgroup compatibility.

## Decision

The first managed-network transport will be designed around Windows integrated authentication through .NET `NegotiateStream`. A connection is usable only when the completed authenticated stream reports all of the following:

- authentication succeeded;
- mutual authentication succeeded;
- data is encrypted;
- data is signed;
- the remote Windows identity is present and passes the configured authorization policy.

The application will request `EncryptAndSign` and then verify the effective stream properties. A successful handshake alone is insufficient. A negotiation that falls back to a mechanism without mutual authentication is rejected.

The authenticated transport identity is authoritative. Protocol v1 intentionally contains no sender-name or sender-identity field.

The follow-up receiver must validate the target service principal name and Kerberos behavior in the intended domain deployment before binding a LAN interface. It must not ship a production listener that only works by accepting an unverified NTLM fallback.

## Why not mutual TLS first

Mutual TLS can support managed networks that do not use Kerberos, but it moves identity lifecycle into certificate operations. A safe deployment needs:

- a trusted issuing CA and explicit client/server certificate purposes;
- private-key protection and per-device enrollment;
- hostname validation and certificate-chain validation;
- expiry monitoring, renewal, revocation, and rollback;
- administrator-controlled trust distribution.

Unskip will not generate a private CA, trust self-signed peer certificates automatically, or store exportable shared client keys. Mutual TLS remains a future enterprise adapter once certificate provisioning and revocation are owned by an administrator.

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

- Domain deployment and SPN behavior must be proven before enabling the receiver.
- Workgroup machines are not silently downgraded to unauthenticated messaging.
- Message payloads cannot impersonate another Windows identity.
- Protocol parsing and admission can be tested without opening sockets.
- This decision adds no listener, firewall rule, startup entry, service, discovery, remote overlay activation, or production send-path change.

## References

- [Microsoft: `NegotiateStream`](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.negotiatestream?view=net-10.0)
- [Microsoft: `AuthenticateAsClient` and effective protection checks](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.negotiatestream.authenticateasclient?view=net-10.0)
- [Microsoft: `SslStream` client authentication](https://learn.microsoft.com/en-us/dotnet/api/system.net.security.sslstream.authenticateasclient?view=net-10.0)
- [Microsoft: Windows Firewall rules](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)
