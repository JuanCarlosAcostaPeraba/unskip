# Architecture

## Current structure

Unskip uses .NET 10 LTS, WPF, C#, and an MVVM-oriented separation:

- `Unskip.Core` contains platform-independent domain and application-facing code.
- `Unskip.Infrastructure.Persistence` contains Entity Framework Core, SQLite mappings, migrations, and per-user path resolution.
- `Unskip.Infrastructure.Windows` contains the direct `msg.exe` process boundary and depends on core.
- `Unskip.App` contains the Windows WPF presentation layer and view models.
- Each production project has a corresponding xUnit test project.

Dependencies point inward: both infrastructure projects reference core, while core has no WPF, Entity Framework Core, or infrastructure dependency. The WPF composition root applies pending local database migrations, constructs `DeviceDirectoryService` and `SendHistoryService`, and injects the SQLite repositories and native message sender before showing its first window.

Release packaging publishes `Unskip.App` as a self-contained `win-x64` folder and wraps it in a per-user NSIS installer. Installation binaries live under `%LOCALAPPDATA%\Programs\Unskip`; mutable application data remains separately owned by the persistence layer under `%LOCALAPPDATA%\Unskip`. This boundary makes upgrades and uninstall non-destructive to user data.

The device UI follows MVVM. `DeviceDirectoryViewModel` owns search, selection, editing, validation presentation, favorites, and one-time destination state. Commands expose asynchronous persistence operations, while the window code-behind only initializes WPF and assigns its injected data context. A small dialog service isolates explicit delete confirmation from view-model behavior.

`MainWindowViewModel` coordinates the Devices, Send, and History workspaces and the destination handoff into `MessageComposerViewModel`. The composer owns draft validation, asynchronous send state, honest delivery-result presentation, optional sanitized diagnostics, and retry behavior. It depends only on the core `IMessageSender` seam; the composition root supplies the Windows implementation.

`ApplicationUpdateViewModel` owns the explicit check, download, verification, and install states. `GitHubReleaseUpdateService` accepts only the latest stable release, exact repository asset paths, the expected NSIS filename, bounded download sizes, and the published SHA-256 checksum. `SemanticVersion` comparison lives in core. The verified installer process boundary lives in `Unskip.Infrastructure.Windows`, uses `UseShellExecute = false`, and has no command arguments or shell expansion. Update checks are never part of startup and failures do not affect offline application behavior.

The visible developer portfolio link is a fixed HTTPS URI exposed by `MainWindowViewModel`. An injected `IExternalUriLauncher` seam delegates it to the Windows default browser association after validating the URI; failures are reported in the sidebar without crashing the application.

## Domain and infrastructure boundaries

Destination and message validation live in core behind `IMessageSender`. Device rules and CRUD orchestration live in core behind `IDeviceRepository`; SQLite implements that contract in infrastructure. Windows process execution is isolated behind an internal invoker so deterministic tests cannot accidentally send real messages.

The delivery boundary accepts validated hostnames and canonical dotted-decimal IPv4 addresses. Hostnames pass through directly. IPv4 destinations cross an injected DNS seam that performs reverse lookup and verifies that the resulting hostname resolves forward to the original address. The process boundary receives only a validated computer name as a separate `/SERVER:` argument; Windows remains authoritative for reachability, permissions, and native acceptance.

Historical send rows keep alias and destination snapshots. Their optional device foreign key uses `SET NULL`, so editing or deleting a directory entry cannot rewrite or remove historical context. Message bodies are not part of the persistence schema.

`SendHistoryService` creates timestamped records through `ISendHistoryRepository`. SQLite stores both available technical targets, the selected destination, result metadata, message length, and a bounded sanitized diagnostic summary. `SendHistoryViewModel` owns local filtering and deletion; destination reuse deliberately opens an empty composer because message bodies are never retained.
