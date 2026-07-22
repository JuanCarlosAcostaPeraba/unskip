# Privacy

Unskip is local-first. It has no central server, cloud backend, analytics, or telemetry.

The device directory belongs to the current Windows user and is stored at `%LOCALAPPDATA%\Unskip\unskip.db`. It can contain aliases, computer names, IPv4 addresses, descriptions, favorites, and timestamps. The database is not synchronized or transmitted by Unskip. It relies on the current Windows account and filesystem permissions; application-level encryption at rest is not currently provided.

The history schema stores destination and alias snapshots so device edits are safe, but it does not store message bodies. Runtime databases, exports, logs containing user data, and personal network details must never be committed.

Windows native messaging cannot prove that a recipient read, displayed, or acknowledged a message. The interface and history must not imply otherwise. Any future proposal to retain message bodies requires an explicit privacy decision before implementation.
