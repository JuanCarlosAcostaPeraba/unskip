# Testing

Run the complete deterministic test suite from the repository root with one command:

```powershell
dotnet test Unskip.sln
```

Tests must not contact or send messages to real network devices. Future real `msg.exe` integration tests must be explicitly configured, opt-in, and disabled by default.

Process timeout, cancellation, output capture, and exit-code behavior are tested with `Unskip.TestProcess`, a local helper that never accesses the network or invokes `msg.exe`.

SQLite integration tests create isolated databases under the system temporary directory, apply the real migrations, and remove those databases after each test. They never read or modify `%LOCALAPPDATA%\Unskip\unskip.db`.

Device-directory view-model tests use an in-memory repository, injected clock, and fake deletion confirmation. They cover cross-field search, saved and manual destination resolution, validation placement, create/edit/delete/favorite operations, and honest handoff to the composer.

Message-composer tests inject fake senders. They cover visible resolved destinations, length validation, duplicate-submit protection, accurate result states, retry with draft preservation, optional technical details, and the documented IPv4 rejection. No view-model test starts `msg.exe`.

Restore the repository-local Entity Framework tool and list migrations with:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations list `
  --project src/Unskip.Infrastructure.Persistence/Unskip.Infrastructure.Persistence.csproj `
  --startup-project src/Unskip.Infrastructure.Persistence/Unskip.Infrastructure.Persistence.csproj
```
