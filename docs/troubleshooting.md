# Troubleshooting

## The required SDK is not found

Install a stable .NET 10 SDK and run `dotnet --info`. The repository's `global.json` rejects preview SDKs and rolls forward within stable .NET 10 feature bands.

## Restore fails

Confirm that the development machine can reach NuGet.org and that its NuGet configuration is readable. Runtime use of Unskip does not require Internet access; package restore is a development/build operation.

## WPF does not build

Build on a supported Windows version with the .NET 10 SDK. WPF is Windows-only. If using Visual Studio, install the .NET desktop development workload.

## The application opens but cannot send

Issue #2 provides only the application shell. Native delivery is deliberately scheduled for issue #3.
