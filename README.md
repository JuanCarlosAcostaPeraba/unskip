# Unskip

Unskip is an unofficial, Windows-only desktop application for sending native messages to Windows computers on an accessible local network. The MVP is designed to work without a central server, cloud backend, Internet connection at runtime, or recipient-side installation.

> Unskip is an independent community project. It is not affiliated with, endorsed by, or sponsored by Microsoft or any employer.

## Current status

The repository contains a .NET 10 LTS WPF application, the secure native delivery engine from issue #3, local device persistence from issue #4, the visual device directory from issue #5, and the message-composer workflow from issue #6.

## Requirements

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio with the .NET desktop development workload is optional

.NET 10 is an LTS release supported through November 2028. `global.json` accepts stable .NET 10 feature bands while preventing preview SDK selection.

## Build and test

From the repository root:

```powershell
dotnet restore Unskip.sln
dotnet build Unskip.sln --no-restore
dotnet test Unskip.sln --no-build
dotnet format Unskip.sln --verify-no-changes --no-restore
```

Run the shell on Windows:

```powershell
dotnet run --project src/Unskip.App/Unskip.App.csproj
```

These commands are development instructions only. The application does not invoke PowerShell, `cmd.exe`, or a shell at runtime.

## Project structure

```text
src/
  Unskip.App/          WPF presentation and view models
  Unskip.Core/         Domain and application-facing abstractions
  Unskip.Infrastructure.Persistence/  Per-user SQLite storage and migrations
  Unskip.Infrastructure.Windows/  Isolated Windows process integration
tests/
  Unskip.App.Tests/    Presentation-layer tests
  Unskip.Core.Tests/   Core tests
  Unskip.Infrastructure.Persistence.Tests/  SQLite migration and CRUD tests
  Unskip.Infrastructure.Windows.Tests/  Native boundary and process-lifecycle tests
  Unskip.TestProcess/  Safe local helper used only by tests
docs/                  Architecture, privacy, security, and support notes
```

See [CONTRIBUTING.md](CONTRIBUTING.md) before proposing changes. Product work is tracked in [epic #1](https://github.com/JuanCarlosAcostaPeraba/unskip/issues/1).

## License

Licensed under the [Apache License 2.0](LICENSE).
