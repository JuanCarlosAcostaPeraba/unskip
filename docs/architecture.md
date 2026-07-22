# Architecture

## Current structure

Unskip uses .NET 10 LTS, WPF, C#, and an MVVM-oriented separation:

- `Unskip.Core` contains platform-independent domain and application-facing code.
- `Unskip.Infrastructure.Persistence` contains Entity Framework Core, SQLite mappings, migrations, and per-user path resolution.
- `Unskip.Infrastructure.Windows` contains the direct `msg.exe` process boundary and depends on core.
- `Unskip.App` contains the Windows WPF presentation layer and view models.
- Each production project has a corresponding xUnit test project.

Dependencies point inward: both infrastructure projects reference core, while core has no WPF, Entity Framework Core, or infrastructure dependency. The WPF composition root applies pending local database migrations, constructs `DeviceDirectoryService`, and injects the device-directory view model before showing its first window.

The device UI follows MVVM. `DeviceDirectoryViewModel` owns search, selection, editing, validation presentation, favorites, and one-time destination state. Commands expose asynchronous persistence operations, while the window code-behind only initializes WPF and assigns its injected data context. A small dialog service isolates explicit delete confirmation from view-model behavior.

## Planned boundaries

Destination and message validation live in core behind `IMessageSender`. Device rules and CRUD orchestration live in core behind `IDeviceRepository`; SQLite implements that contract in infrastructure. Windows process execution is isolated behind an internal invoker so deterministic tests cannot accidentally send real messages.

The current delivery boundary supports documented hostname targets only. The directory can store a canonical IPv4 address, but delivery rejects IPv4 until compatible `msg.exe` behavior is verified in a controlled Windows environment.

Historical send rows keep alias and destination snapshots. Their optional device foreign key uses `SET NULL`, so editing or deleting a directory entry cannot rewrite or remove historical context. Message bodies are not part of the persistence schema.
