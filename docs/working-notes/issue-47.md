# Issue 47 working note

- Added an explicit local preview entry point to the existing message composer.
- Kept the preview independent from the selected destination, draft, sender, and history.
- Covered the Windows virtual screen, including negative monitor coordinates, with per-monitor DPI awareness.
- Added a fixed accessible close button, Escape, Alt+F4, and a one-minute safety timeout.
- Closed the overlay when display topology changes to avoid leaving an orphaned window.
- Prevented simultaneous preview, send, and back-navigation actions.
- Added deterministic timeout, dismissal, rendering, geometry, no-send, and no-persistence tests.
- Deliberately excluded networking, listeners, firewall rules, services, startup registration, desktop capture, system-shortcut suppression, moving controls, and forced reopening.
