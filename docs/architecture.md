# Architecture

## Current structure

Unskip uses .NET 10 LTS, WPF, C#, and an MVVM-oriented separation:

- `Unskip.Core` contains platform-independent domain and application-facing code.
- `Unskip.Infrastructure.Windows` contains the direct `msg.exe` process boundary and depends on core.
- `Unskip.App` contains the Windows WPF presentation layer and view models.
- Each production project has a corresponding xUnit test project.

Dependencies point inward: Windows infrastructure references core, while core has no WPF or infrastructure dependency. The initial shell binds directly to `MainWindowViewModel`; code-behind only initializes generated WPF components. The app will compose the sender when the visual sending workflow is implemented.

## Planned boundaries

Destination and message validation live in core behind `IMessageSender`. Windows process execution is isolated behind an internal invoker so deterministic tests cannot accidentally send real messages. Later issues will add separate persistence and history boundaries. SQLite belongs to infrastructure, not WPF code-behind.

The current delivery boundary supports documented hostname targets only. IPv4 input is rejected until compatible `msg.exe` behavior is verified in a controlled Windows environment. No persistence implementation exists yet.
