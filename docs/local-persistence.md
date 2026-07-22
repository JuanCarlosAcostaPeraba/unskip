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

The initial schema includes send-history metadata without message bodies. A history row stores alias and destination snapshots plus an optional device relationship. Deleting a device sets that relationship to `NULL`; it does not delete or rewrite the snapshot.

## Privacy and operations

The database remains local and is never synchronized or transmitted by Unskip. It contains no credentials, secrets, or message bodies. Database files and SQLite sidecar files are ignored by Git.

Development and test data must always be fictitious. Unskip is unofficial and is not affiliated with Microsoft or any employer.
