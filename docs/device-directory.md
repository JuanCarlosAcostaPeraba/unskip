# Device directory and destination picker

The `Devices` workspace provides a local visual directory without requiring terminal commands. Users can create, edit, favorite, search, and delete saved devices. An alias can represent a person, room, shared workstation, or any other meaningful destination.

Search covers alias, computer name, IPv4 address, and description. The selected alias and resolved technical destination are shown together before the message workflow can continue. If both a computer name and IPv4 address are stored, the chosen preferred destination is respected.

One-time destinations remain usable without saving. A valid hostname or canonical IPv4 address can be prepared directly, and the interface offers a non-blocking `Save as device` action. The destination state includes its saved device identifier, technical value, and destination kind so issue #6 can consume it without parsing display text.

Deleting a device requires explicit confirmation. Historical alias and destination snapshots remain intact through the persistence behavior implemented in issue #4.

The interface uses keyboard-focus indicators, accessible names, and the active Windows light/dark preference. High-contrast mode uses Windows system brushes. No message is sent from this workspace yet, and the UI states that the composer belongs to issue #6.

Unskip is unofficial and is not affiliated with Microsoft or any employer. All examples and test data are fictitious.
