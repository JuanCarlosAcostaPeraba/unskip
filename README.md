# Unskip

<img src="assets/unskip-logo.svg" alt="Unskip logo" width="112" height="112" />

[![CI](https://github.com/JuanCarlosAcostaPeraba/unskip/actions/workflows/ci.yml/badge.svg)](https://github.com/JuanCarlosAcostaPeraba/unskip/actions/workflows/ci.yml)

Unskip is an unofficial, Windows-only desktop application for sending native messages to Windows computers on an accessible local network. The MVP is designed to work without a central server, cloud backend, Internet connection at runtime, or recipient-side installation.

> Unskip is an independent community project. It is not affiliated with, endorsed by, or sponsored by Microsoft or any employer.

## Current status

The repository contains a working .NET 10 LTS WPF application, secure native delivery, local device persistence, a visual device directory, the message composer, privacy-conscious local send history, Windows CI, and a reproducible Windows release pipeline. Published releases provide a per-user installer and portable archive. Early releases are unsigned and may trigger Microsoft Defender SmartScreen.

Start with the [user guide](docs/user-guide.md) for prerequisites, the first message, saved devices, history, limitations, backup, and deletion.

## Runtime prerequisites

- Windows 10 or Windows 11
- Windows `msg.exe` available on the sending computer
- A destination computer name, compatible active session, required Windows permission, and permitted network path

The recipient does not install Unskip or create an account. Unskip has no central server, cloud backend, telemetry, runtime Internet requirement, or sound. A successful native request is never proof that a message was read or acknowledged.

The current pre-release source build additionally requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). Visual Studio with the .NET desktop development workload is optional.

End users should download only from [GitHub Releases](https://github.com/JuanCarlosAcostaPeraba/unskip/releases) and verify the published SHA-256 checksum. The self-contained x64 build does not require a separate .NET installation.

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

## Documentation

- [User guide](docs/user-guide.md)
- [Troubleshooting](docs/troubleshooting.md)
- [Privacy](docs/privacy.md) and [local data](docs/local-persistence.md)
- [Security design](docs/security.md) and [threat model](docs/threat-model.md)
- [Architecture](docs/architecture.md) and [testing](docs/testing.md)
- [Release and packaging process](docs/releasing.md)

## License

Licensed under the [Apache License 2.0](LICENSE).
