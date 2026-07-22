# Local device persistence

Unskip stores its device directory in a SQLite database owned by the current Windows user:

```text
%LOCALAPPDATA%\Unskip\unskip.db
```

`UnskipDatabase.ForCurrentUser()` resolves that path. `InitializeAsync()` creates the parent directory and applies every pending Entity Framework Core migration. The WPF application performs this initialization during startup; users do not need to run database commands.

## Stored device data

Each device has a stable identifier, required alias, optional computer name, optional canonical IPv4 address, optional description, favorite flag, preferred destination, and created, updated, and last-used timestamps. At least one technical destination is required.

Aliases are compared using an invariant normalized key. Computer names are normalized to lowercase, and IPv4 addresses must use canonical dotted-decimal notation. Unique database indexes reject duplicate aliases, computer names, and IPv4 addresses with a clear conflict result.

## History integrity

The schema includes send-history metadata without message bodies. A history row stores alias, computer-name, IPv4, selected-destination, timestamp, result, duration, exit-code, message-length, and sanitized-diagnostic snapshots plus an optional device relationship. Deleting a device sets that relationship to `NULL`; it does not delete or rewrite the snapshot.

## Backup, restore, and deletion

Close Unskip before copying, restoring, renaming, or deleting its data. Back up the complete `%LOCALAPPDATA%\Unskip` directory because SQLite may use `unskip.db-wal` and `unskip.db-shm` sidecar files alongside `unskip.db`.

Restoring the complete directory while Unskip is closed preserves the device directory and history. Treat the backup as sensitive because it can identify local destinations even though it has no message bodies or credentials.

Deleting the complete directory removes devices and history and is irreversible without a backup. Unskip creates a new database on the next launch. Clearing history in the UI removes history only; deleting the application binaries does not necessarily remove per-user data.

## Privacy and operations

The database remains local and is never synchronized or transmitted by Unskip. It contains no credentials, secrets, or message bodies. Application-level encryption at rest is not currently provided; protection relies on the Windows account and filesystem permissions. Database files and SQLite sidecar files are ignored by Git.

Development and test data must always be fictitious. Unskip is unofficial and is not affiliated with Microsoft or any employer.
