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

The urgent-attention prototype remains entirely inside `Unskip.App`. `IUrgentAttentionPreviewService` isolates the composer from WPF window creation, while `IVirtualScreenProvider` provides testable virtual-screen and primary-attention geometry. A borderless WPF window uses the full Windows virtual-screen rectangle, including negative monitor coordinates, while translating the message card to the primary screen so it cannot be split across a monitor seam. Its message-first card exposes only the local source status, a large scrollable message, and a fixed close action; the automatic timeout and keyboard dismissal paths remain active without adding visual noise. Per-monitor DPI awareness is declared in the application manifest. Display-topology changes close the preview rather than risking an orphaned overlay.

`ApplicationUpdateViewModel` owns the explicit check, download, verification, and install states. `GitHubReleaseUpdateService` accepts only the latest stable release, exact repository asset paths, the expected NSIS filename, bounded download sizes, and the published SHA-256 checksum. `SemanticVersion` comparison lives in core. The verified installer process boundary lives in `Unskip.Infrastructure.Windows`, uses `UseShellExecute = false`, and has no command arguments or shell expansion. Update checks are never part of startup and failures do not affect offline application behavior.

The visible developer portfolio link is a fixed HTTPS URI exposed by `MainWindowViewModel`. An injected `IExternalUriLauncher` seam delegates it to the Windows default browser association after validating the URI; failures are reported in the sidebar without crashing the application.

UI localization belongs to `Unskip.App`. Neutral `Strings.resx` resources define English and `Strings.es.resx` supplies the Spanish satellite catalog; an automated test requires both catalogs to expose exactly the same keys. XAML uses a small markup extension, while view models and dialog services resolve dynamic text through the same resource manager. Startup selects a validated persisted preference or the supported Windows UI language, falling back to English. Language changes are explicit, persisted below `%LOCALAPPDATA%\Unskip`, and applied through a direct executable restart with `UseShellExecute = false`; the restart confirmation warns that drafts are not persisted.

Resident behavior remains a presentation-layer concern. `ResidentApplicationController` coordinates testable window and notification-area seams, while `NotifyIconTrayService` is the only Windows Forms adapter and is used solely for the native notification icon and menu. WPF remains responsible for every application window. Normal close requests hide reusable windows; a shared `ApplicationExitState` allows tray exit, update installation, language restart, and Windows session ending to bypass close-to-tray interception and shut down cleanly.

`QuickSendViewModel` loads saved devices through the existing `DeviceDirectoryService` and delegates message validation, sending, result wording, retries, and history persistence to a dedicated `MessageComposerViewModel`. The compact window therefore introduces no second transport path and stores no message body. Notification-area state deliberately has no pending count because no receiver-side durable state exists yet.

The future LAN protocol foundation lives in `Unskip.Core.Messaging.Lan` and has no socket or WPF dependency. `LanProtocolFrameCodec` applies bounded big-endian length framing and strict deterministic UTF-8 JSON. `LanMessageRequestValidator` enforces protocol version, UTC freshness, lifetime, nonce, kind, and the existing message policy. `AuthenticatedSessionValidator` converts only a fully authenticated, mutually authenticated, encrypted, signed transport context into an authoritative sender identity. `LanReceiverAdmissionService` applies per-identity rate limiting and independent message-ID/nonce replay protection before returning the honest **accepted for local handling** status.

No production code constructs those protocol components yet. The current `IMessageSender` composition still selects `WindowsMsgSender`, and no listener, firewall change, startup entry, remote overlay dispatch, incoming persistence, pending count, or conversation model has been introduced. [ADR 0001](decisions/0001-authenticated-lan-transport.md) records the transport decision and deployment gates.

## Domain and infrastructure boundaries

Destination and message validation live in core behind `IMessageSender`. Device rules and CRUD orchestration live in core behind `IDeviceRepository`; SQLite implements that contract in infrastructure. Windows process execution is isolated behind an internal invoker so deterministic tests cannot accidentally send real messages.

The delivery boundary accepts validated hostnames and canonical dotted-decimal IPv4 addresses. Hostnames pass through directly. IPv4 destinations cross an injected DNS seam that performs reverse lookup and verifies that the resulting hostname resolves forward to the original address. The process boundary receives only a validated computer name as a separate `/SERVER:` argument; Windows remains authoritative for reachability, permissions, and native acceptance.

Historical send rows keep alias and destination snapshots. Their optional device foreign key uses `SET NULL`, so editing or deleting a directory entry cannot rewrite or remove historical context. Message bodies are not part of the persistence schema.

`SendHistoryService` creates timestamped records through `ISendHistoryRepository`. SQLite stores both available technical targets, the selected destination, result metadata, message length, and a bounded sanitized diagnostic summary. `SendHistoryViewModel` owns local filtering and deletion; destination reuse deliberately opens an empty composer because message bodies are never retained.
