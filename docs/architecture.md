# Architecture

## Current structure

Unskip uses .NET 10 LTS, WPF, C#, and an MVVM-oriented separation:

- `Unskip.Core` contains platform-independent domain and application-facing code.
- `Unskip.App` contains the Windows WPF presentation layer and view models.
- Each production project has a corresponding xUnit test project.

Dependencies point inward: the app references core, while core has no WPF or infrastructure dependency. The initial shell binds directly to `MainWindowViewModel`; code-behind only initializes generated WPF components.

## Planned boundaries

Later issues will introduce explicit boundaries for destination validation, message delivery, local persistence, and history. Windows process integration will remain behind an interface so unit tests cannot send real messages. SQLite belongs to infrastructure, not WPF code-behind.

Issue #2 intentionally contains no `msg.exe` integration and no persistence implementation.
