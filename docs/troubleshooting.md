# Troubleshooting

## The required SDK is not found

Install a stable .NET 10 SDK and run `dotnet --info`. The repository's `global.json` rejects preview SDKs and rolls forward within stable .NET 10 feature bands.

## Restore fails

Confirm that the development machine can reach NuGet.org and that its NuGet configuration is readable. Runtime use of Unskip does not require Internet access; package restore is a development/build operation.

## WPF does not build

Build on a supported Windows version with the .NET 10 SDK. WPF is Windows-only. If using Visual Studio, install the .NET desktop development workload.

## The application opens but cannot send

The native sender exists but is not connected to the WPF composer until issue #6. When connected, Windows may still reject delivery if `msg.exe` is unavailable, the sender lacks Message permission, the target cannot be contacted, or no compatible active session is available. Unskip will report the category and sanitized technical details without claiming that a person read the message.

## The local database cannot be opened

Confirm that the current Windows account can write to `%LOCALAPPDATA%\Unskip`. Close Unskip before copying or moving `unskip.db`, `unskip.db-shm`, or `unskip.db-wal`. Renaming the entire `Unskip` directory is a recoverable way to let the application create a fresh database while preserving the original for investigation.
