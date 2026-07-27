# Testing

Run the complete deterministic test suite from the repository root with one command:

```powershell
dotnet test Unskip.sln
```

Tests must not contact or send messages to real network devices. The deterministic suite excludes the `NativeIntegration` category and never starts `msg.exe`.

Process timeout, cancellation, output capture, and exit-code behavior are tested with `Unskip.TestProcess`, a local helper that never accesses the network or invokes `msg.exe`.

IPv4 delivery tests use an injected DNS lookup and reserved documentation addresses. They cover hostname passthrough, forward-confirmed reverse lookup, missing PTR data, mismatched forward lookup, cancellation, and timeout without contacting a real DNS server.

SQLite integration tests create isolated databases under the system temporary directory, apply the real migrations, and remove those databases after each test. They never read or modify `%LOCALAPPDATA%\Unskip\unskip.db`.

Device-directory view-model tests use an in-memory repository, injected clock, and fake deletion confirmation. They cover cross-field search, saved and manual destination resolution, validation placement, create/edit/delete/favorite operations, and honest handoff to the composer.

Message-composer tests inject fake senders. They cover visible resolved destinations, hostname and canonical IPv4 validation, length validation, duplicate-submit protection, accurate result states, retry with draft preservation, and optional technical details. No view-model test starts `msg.exe`.

Urgent-overlay tests use injected delay and geometry seams. They verify bounded timeout validation, single dismissal, cancellation, negative virtual-screen coordinates, primary-monitor card placement, fixed accessible dismissal controls, Escape and Alt+F4, invalid-content rejection before display access, exact in-memory draft handoff, and that a local preview neither invokes the sender nor creates history.

History tests use injected clocks and in-memory repositories for MVVM behavior, plus temporary migrated SQLite databases for full metadata round trips, deletion, clearing, and immutable snapshots after device edits or deletion. Test message bodies are verified absent from persisted diagnostics.

## Native integration test

The native `msg.exe` test is disabled by default and is never enabled in CI. It sends a real message only when both environment variables below are deliberately configured:

```powershell
$env:UNSKIP_RUN_NATIVE_INTEGRATION_TESTS = "1"
$env:UNSKIP_NATIVE_TEST_TARGET = "lab-pc-01"
dotnet test tests/Unskip.Infrastructure.Windows.Tests `
  --filter "Category=NativeIntegration"
```

Use only a fictitious or dedicated test target that you own or are authorized to contact. Unset both variables after testing. The normal `dotnet test Unskip.sln` command discovers this test as skipped and remains local-only.

## Mutual-TLS infrastructure tests

The mutual-TLS infrastructure tests perform real client/server `SslStream` handshakes over local named pipes. They use a private ephemeral test CA, fictitious subjects, temporary per-user Schannel key containers, custom trust held only by the test process, and no Windows trust-store mutation. The suite verifies encrypted transfer plus unauthorized fingerprint, DNS-name, missing-client-certificate, and expired-certificate rejection. It never binds a TCP port.

## Continuous integration

GitHub Actions restores, verifies formatting and analyzers, builds Release, runs the deterministic suite on Windows, audits vulnerable NuGet dependencies, and uploads TRX and coverage results. Pull requests also receive GitHub dependency review. Dependabot checks NuGet and GitHub Actions dependencies weekly.

Restore the repository-local Entity Framework tool and list migrations with:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef migrations list `
  --project src/Unskip.Infrastructure.Persistence/Unskip.Infrastructure.Persistence.csproj `
  --startup-project src/Unskip.Infrastructure.Persistence/Unskip.Infrastructure.Persistence.csproj
```
