# Issue 55 working notes

## Scope

- Add complete English and Spanish presentation resources.
- Choose a supported Windows UI language on first run and otherwise fall back to English.
- Persist an explicit per-user language choice locally.
- Apply a language change after a confirmed direct application restart.
- Cover resource parity, language selection, persistence, and restart behavior with tests.

## Product decisions

- The selector uses compact EN and ES controls in the existing sidebar.
- A restart keeps static XAML, dynamic view-model text, dialogs, accessibility labels, and the urgent preview on one consistent culture.
- The confirmation explicitly warns that the current message draft is not saved.
- Native diagnostic text remains unchanged so troubleshooting data is not mistranslated.

## Deliberate exclusions

- Background and system-tray lifecycle.
- Quick-send panel and pending-message indicator.
- Recipient-side transport, queues, acknowledgements, and conversation state.

Those capabilities remain separate roadmap phases because they change the application lifecycle and delivery architecture.
