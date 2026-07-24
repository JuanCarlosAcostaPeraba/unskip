# Issue 49 working note

- Passed the current validated composer draft to the local overlay preview without invoking delivery or persistence boundaries.
- Reused core message validation for empty, oversized, and unsupported-control-character rejection.
- Kept preview, send, and back-navigation states mutually exclusive.
- Preserved fixed dismissal, keyboard shortcuts, timeout, display-change closure, virtual-screen geometry, and per-monitor DPI safeguards.
- Used the local packaged-build smoke test to find and fix a card positioned across the physical seam between two monitors; the card now stays wholly on the primary display.
- Updated the product version to `0.3.0`.
- Added user-facing release notes that distinguish the IPv4 delivery improvement from the local-only overlay preview.
- Kept receiver networking, firewall changes, services, startup registration, forced overlays, and acknowledgement claims out of scope.
