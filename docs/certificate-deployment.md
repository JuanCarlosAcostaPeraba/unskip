# Receiver certificate deployment

The Unskip receiver is not active yet. This document defines the minimum certificate policy that a future mutual-TLS adapter must enforce before any LAN listener is approved.

## Ownership

An organization's administrator owns:

- the issuing certification authority and trust distribution;
- server and client certificate enrollment;
- private-key protection;
- certificate inventory, expiry monitoring, renewal, revocation, and removal;
- the exact sender fingerprints authorized on each recipient;
- firewall and application deployment policy.

Unskip does not create a certification authority, generate production identities, install roots, accept arbitrary self-signed certificates, enroll certificates, or persist private keys.

## Required validation

The future `SslStream` adapter must use operating-system certificate validation and require:

- a chain to an explicitly trusted organizational or public root;
- current certificate validity;
- online revocation checking with a bounded handshake timeout, subject to documented organizational policy;
- DNS hostname validation for the receiver certificate;
- Server Authentication EKU (`1.3.6.1.5.5.7.3.1`) on the receiver certificate;
- Client Authentication EKU (`1.3.6.1.5.5.7.3.2`) on sender certificates;
- a client certificate on every receiver connection;
- successful mutual authentication, encryption, and signing after the handshake.

Production code must not use a callback that returns `true` unconditionally, bypass chain errors, accept name mismatch, downgrade protocol protection, or fall back to an unauthenticated channel.

The implemented stream authenticator takes ownership of an already-connected duplex stream. The public production path uses operating-system protocol selection and online revocation checking. A server handshake requires the client certificate; a client handshake validates the configured DNS target. After the handshake, Unskip still verifies mutual authentication, encryption, signing, and exact fingerprint authorization before exposing the protected stream.

Authentication failure closes the TLS stream and its underlying connection because a partially authenticated connection is not reusable. Cancellation and the bounded handshake timeout also fail closed.

## Authorization identity

Certificate subjects, common names, friendly names, and organization fields are display metadata. They are not authorization keys.

After successful chain and purpose validation, Unskip calculates the SHA-256 fingerprint of the complete peer certificate DER bytes. The canonical lowercase hexadecimal fingerprint is prefixed with `mtls-sha256:` and used for:

- exact local allow-list decisions;
- replay-cache partitioning;
- per-identity rate limiting.

The allow-list is an immutable snapshot while a receiver session is running. Changes require explicit policy reload in a future implementation.

## Renewal and revocation

Exact certificate fingerprints normally change at renewal. A safe planned rotation is:

1. issue the replacement certificate without removing the current certificate;
2. add both old and new fingerprints to the administrator-controlled allow-list;
3. deploy the replacement and verify authenticated connectivity;
4. remove the old fingerprint;
5. revoke the old certificate when appropriate and confirm revocation information is available.

For compromise or emergency removal, revoke the certificate and remove its fingerprint immediately. Fingerprint removal is the local authorization control; revocation protects every system that trusts the issuing CA.

Backups and diagnostics must never include private keys. Real subjects, fingerprints, identities, certificates, or organizational hostnames must not be committed to the repository.

## Development tests

Unit tests use fictitious identities and create a private ephemeral CA for real `SslStream` handshakes over local named pipes. On Windows, Schannel cannot use purely ephemeral private-key handles, so leaf certificates are re-imported into temporary per-user key containers for the lifetime of each test and deleted on disposal. They are never added to the certificate trust store. The test-only custom-root validator and relaxed revocation setting are internal and unavailable through the production constructor.
