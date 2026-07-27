# Issue 59 working note

- Defined protocol v1 in core without adding a socket, listener, service, firewall rule, startup entry, discovery mechanism, or production send-path change.
- Kept sender identity out of the request payload and required it to come from a mutually authenticated, encrypted, and signed transport context.
- Added bounded big-endian framing, strict deterministic JSON, explicit versioning, UTC freshness and expiry, a 128-bit nonce, message bounds, and honest receiver responses.
- Added independent message-ID and nonce replay detection with a bounded five-minute cache.
- Added bounded per-identity fixed-window rate limiting.
- Made replay and rate-limit capacity exhaustion fail closed.
- Recorded Windows integrated authentication as the managed-network direction and mutual TLS as a future administrator-provisioned alternative.
- Deferred SPN validation, allow-list deployment, real sockets, firewall policy, receiver UI activation, incoming persistence, pending state, and conversations to reviewed follow-up issues.
