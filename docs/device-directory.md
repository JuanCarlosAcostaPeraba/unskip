# Device directory and destination picker

The `Devices` workspace provides a local visual directory without requiring terminal commands. Users can create, edit, favorite, search, and delete saved devices. An alias can represent a person, room, shared workstation, or any other meaningful destination.

Search covers alias, computer name, IPv4 address, and description. The selected alias and resolved technical destination are shown together before the message workflow can continue. If both a computer name and IPv4 address are stored, the chosen preferred destination is respected.

One-time destinations remain usable without saving. A valid hostname or canonical IPv4 address can be prepared directly, and the interface offers a non-blocking `Save as device` action. The resolved alias, technical value, and destination kind are handed to the message composer without parsing display text.

Deleting a device requires explicit confirmation. Historical alias and destination snapshots remain intact after a device is edited or deleted.

The interface uses keyboard-focus indicators, accessible names, and the active Windows light/dark preference. High-contrast mode uses Windows system brushes. **Prepare message** opens the composer for the resolved hostname or canonical IPv4 destination.

Unskip is unofficial and is not affiliated with Microsoft or any employer. All examples and test data are fictitious.
