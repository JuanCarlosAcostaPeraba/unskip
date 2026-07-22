# Issue #5 working notes

- Replaced the static shell with an injected MVVM device-directory workspace.
- Added search across alias, hostname, IPv4 address, and description.
- Added create, edit, favorite, and explicitly confirmed delete actions.
- Added saved and one-time destination resolution with the actual technical target always visible.
- Added field-level validation and duplicate-conflict feedback.
- Added Windows light/dark and high-contrast palette handling without writing registry settings.
- Added accessible control names and keyboard-focus states.
- Kept actual message delivery out of scope until issue #6 and avoided delivery/read claims.
- Verified the dark-theme layout and accessibility tree by running the compiled WPF application locally.
