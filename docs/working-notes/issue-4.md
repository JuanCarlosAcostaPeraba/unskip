# Issue #4 working notes

- Added device validation and CRUD orchestration to `Unskip.Core`.
- Added a per-user SQLite implementation using Entity Framework Core 10.
- Added an initial reproducible migration and automatic WPF startup initialization.
- Added unique constraints for normalized aliases, hostnames, and IPv4 addresses.
- Added history snapshots with an optional `SET NULL` device relationship and no message body column.
- Pinned `SQLitePCLRaw.bundle_e_sqlite3` 2.1.12 because the transitive 2.1.11 package was rejected by the repository's vulnerability audit.
- Added deterministic domain tests and isolated temporary-database integration tests.
