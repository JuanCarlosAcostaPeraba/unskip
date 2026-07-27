# Issue 63 working note

- Added a real `SslStream` client/server authenticator over a caller-supplied connected duplex stream.
- Kept the production constructor fixed to operating-system protocol selection, online revocation checking, strict system certificate errors, and required EKU.
- Required a client certificate on the server and the expected DNS receiver name on the client.
- Verified mutual authentication, encryption, signing, remote certificate presence, and exact fingerprint authorization after the handshake.
- Returned the protected stream only with a validated core `AuthenticatedSessionContext`.
- Closed failed, cancelled, timed-out, weak, or unauthorized TLS connections.
- Added an internal-only certificate validation seam for the test assembly; production callers cannot bypass validation through it.
- Exercised real encrypted transfer and rejection of unauthorized, wrong-name, missing-client-certificate, and expired-certificate sessions over local named pipes.
- Used fictitious ephemeral certificates and temporary Schannel-compatible user key containers without installing trust or certificates.
- Added no TCP listener, firewall rule, startup entry, certificate enrollment, trust-store mutation, overlay dispatch, incoming persistence, or production sender change.
