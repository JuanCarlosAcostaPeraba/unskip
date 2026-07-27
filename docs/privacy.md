# Privacy

Unskip is local-first. It has no central server, cloud backend, analytics, or telemetry.

Selecting **Check for updates** makes an unauthenticated HTTPS request to the official public GitHub Releases API. Selecting **Download update** retrieves the published installer and checksum from this repository's GitHub Release. These actions send the ordinary network metadata required for an HTTPS request, such as the user's public IP address and HTTP headers, to GitHub. Unskip sends no device-directory entries, message data, history, account token, or telemetry, and it performs no background update polling.

The device directory belongs to the current Windows user and is stored at `%LOCALAPPDATA%\Unskip\unskip.db`. It can contain aliases, computer names, IPv4 addresses, descriptions, favorites, and timestamps. The database is not synchronized or transmitted by Unskip. It relies on the current Windows account and filesystem permissions; application-level encryption at rest is not currently provided.

The English or Spanish interface preference is stored as a short language code in `%LOCALAPPDATA%\Unskip\language.txt`. Changing the language does not contact a server. A missing, unreadable, or unsupported preference is ignored safely.

Notification-area residency and the quick-send window do not add another data store or network service. A quick-send draft stays only in process memory and uses the existing native Windows send boundary when explicitly submitted. The message body is never written to history; completed attempt metadata follows the existing history rules below. Closing the panel hides it and can retain that in-memory draft until the user exits Unskip or Windows ends the process.

The history schema stores immutable alias, hostname, IPv4, selected-destination, result, duration, exit-code, message-length, and sanitized-diagnostic snapshots so device edits are safe. It does not store message bodies. Runtime databases, exports, logs containing user data, and personal network details must never be committed.

Verified update installers are cached below `%LOCALAPPDATA%\Unskip\updates`. Backups of `%LOCALAPPDATA%\Unskip` remain local data and can expose aliases, destinations, and cached installers. Protect them with the same care as the Windows account, copy the complete directory only while Unskip is closed, and delete them intentionally when they are no longer required.

Windows native messaging cannot prove that a recipient read, displayed, or acknowledged a message. The interface and history must not imply otherwise. Any future proposal to retain message bodies requires an explicit privacy decision before implementation.
