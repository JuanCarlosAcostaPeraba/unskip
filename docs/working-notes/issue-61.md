# Issue 61 working note

- Confirmed locally, without persisting any real identity data, that the development computer is domain joined.
- Corrected the assumption that a per-user process can use the computer account's `HOST/...` SPN for mutual Kerberos authentication.
- Kept `NegotiateStream` only for administrator-provisioned SPNs tied to the receiver's actual run-as account.
- Made mutual TLS the default deployable direction for the future per-user receiver.
- Separated the authoritative identity key from the non-authoritative display name.
- Added canonical `windows-sid:` and `mtls-sha256:` identity forms.
- Added strict SHA-256 fingerprint parsing, canonicalization, fixed-time byte equality, and immutable exact allow-listing.
- Re-keyed replay protection and rate limiting by authoritative identity rather than display name.
- Documented certificate chain, hostname, EKU, validity, revocation, renewal, overlap, and removal requirements.
- Added no sockets, TLS handshake, certificate creation, enrollment, trust changes, private-key persistence, startup entry, firewall rule, or production transport change.
