# Issue 57 working notes

## Scope

- Keep Unskip resident in the Windows notification area.
- Hide reusable WPF windows on normal close and restore or activate them on demand.
- Add localized notification-area actions for the full application, quick send, and explicit exit.
- Add a compact quick-send window that reuses the existing composer, sender, and history boundaries.

## Lifecycle decisions

- WPF remains the window framework; Windows Forms is used only for `NotifyIcon`.
- Closing a window is not an exit while resident mode is active.
- Tray exit, verified update installation, language restart, and Windows session ending share an explicit exit state so close interception cannot block them.
- The notification icon and both windows are created once and reused rather than duplicated.

## Product and privacy decisions

- A quick-send draft is memory-only and remains available while the process is resident.
- No message body is added to history or another local store.
- No pending count is displayed because Unskip has no receiver-side durable state.
- Automatic Windows startup, registry changes, toasts, and sounds remain out of scope.
