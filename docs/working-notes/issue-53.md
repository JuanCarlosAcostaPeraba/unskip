# Issue 53 working notes

## Scope

- Make the local urgent preview message-first.
- Simplify the Send workspace and application chrome.
- Preserve honest result wording and every existing overlay dismissal path.

## Deliberate exclusions

- Localization resources.
- Background or system-tray lifecycle.
- Quick-send panel.
- Receiver, pending-message state, and conversation transport.

These belong to the phased roadmap in issue 52 because pending and chat semantics require a real recipient-side protocol rather than presentation-only state.
