# Release and packaging

Unskip publishes a self-contained Windows x64 application, a per-user `.exe` installer, a portable ZIP, and SHA-256 checksums. The runtime requires neither a separate .NET installation nor Node.js, Electron, a server, or Internet access.

## Packaging decisions

- Runtime identifier: `win-x64`.
- Deployment: self-contained, untrimmed .NET publish. Trimming remains disabled because WPF and Entity Framework Core use metadata dynamically.
- Installer: NSIS 3.12, installed from the WinGet catalog and verified by compiler SHA-256 in the release workflow. NSIS is open source and permits commercial use.
- Scope: current user, installed under `%LOCALAPPDATA%\Programs\Unskip` without elevation by default.
- Mutable data: `%LOCALAPPDATA%\Unskip`, outside the installation directory.
- Upgrade identity: stable per-user registry keys in `installer/Unskip.nsi` reuse the existing installation directory.
- Uninstall: removes installed binaries and shortcuts, but intentionally preserves the local device directory and history.
- Signing: disabled until maintainers have a real, verified code-signing identity and protected CI integration.

The portable ZIP contains the same published application files as the installer. Users must extract the complete archive before launching `Unskip.App.exe`.

## Build locally

Install the stable .NET 10 SDK and NSIS 3.12, then run:

```powershell
dotnet restore Unskip.sln
dotnet build Unskip.sln --configuration Release --no-restore
dotnet test Unskip.sln --configuration Release --no-build --no-restore --filter "Category!=NativeIntegration"
./eng/package.ps1 -Version 0.1.0-beta.1
```

The script resets only repository-owned paths below `artifacts`, publishes the application, compiles the installer, creates the portable ZIP, and writes `artifacts/release/SHA256SUMS.txt`.

## Publish from GitHub

The `.github/workflows/release.yml` workflow accepts tags matching `vMAJOR.MINOR.PATCH` with an optional SemVer pre-release suffix. It rejects tags whose commit is not contained in `main`.

1. Merge the release changes into `main` and confirm CI is green.
2. Create an annotated tag such as `v0.1.0-beta.1` on the intended `main` commit.
3. Push the tag.
4. Review the Release workflow. It repeats formatting, build, deterministic tests, and dependency auditing before packaging.
5. Inspect the generated GitHub release, generated changelog notes, filenames, and `SHA256SUMS.txt` before announcing it.

Stable tags create normal releases. A version with a hyphen, such as `v0.1.0-beta.1`, creates a GitHub pre-release. Do not reuse or move a published tag; release artifacts and their checksums are tied to that source commit.

## Signing and SmartScreen

The current workflow does not sign files and the project must not claim otherwise. Unsigned installers and executables can show Microsoft Defender SmartScreen warnings, and organizational policy may block them. Users should download only from this repository, verify checksums, and follow their security policy rather than disabling protections.

When a real certificate or managed signing service becomes available, add signing as a distinct protected step before checksums are generated. Verify the signature after signing and never store a `.pfx`, password, or private key in the repository.

## Upgrade and uninstall verification

Before publishing, verify on a clean or disposable Windows x64 machine:

1. install the previous release without administrator elevation;
2. create a fictitious saved device and history entry;
3. install the candidate release over it;
4. confirm the application shows the candidate version and the fictitious data remains;
5. uninstall through Windows Installed apps;
6. confirm the application files are removed and `%LOCALAPPDATA%\Unskip\unskip.db` remains;
7. reinstall and confirm the preserved data is readable;
8. remove the disposable test data intentionally.

Never use real workplace hosts, addresses, identities, messages, or databases in release verification.

## Future WinGet manifest

Do not submit a WinGet manifest before a stable, reviewed release exists. The intended package identity is `JuanCarlosAcostaPeraba.Unskip`, installer type is Nullsoft, architecture is x64, and scope is user. The release installer supports NSIS's standard `/S` silent switch and has stable versioned URLs and SHA-256 checksums, which are the inputs a future manifest will require.

Generate a candidate manifest from the final stable GitHub release, review every field manually, and submit it to the Windows Package Manager Community Repository as a separate tracked change.

Unskip is unofficial and is not affiliated with Microsoft or any employer.
