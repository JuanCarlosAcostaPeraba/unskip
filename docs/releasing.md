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

Development is integrated into `dev`. The `main` branch is reserved for releases, and each new commit merged into `main` starts `.github/workflows/release.yml` automatically.

1. Merge completed feature and dependency pull requests into `dev` and confirm CI is green.
2. Open a release pull request from `dev` into `main`, review the complete change set, and wait for its checks.
3. Merge the pull request. Do not edit a version file or push a release tag.
4. The Release workflow calculates the next stable version, repeats formatting, build, deterministic tests, and dependency auditing, then packages, tags, and publishes the GitHub release.
5. Inspect the generated release notes, filenames, and `SHA256SUMS.txt` before announcing the release.

Stable tags use `vMAJOR.MINOR.PATCH`. While Unskip is in `0.x`, each new release increments the minor component and resets the patch component, so `v0.3.0` is followed by `v0.4.0`. Starting with `v1.0.0`, automatic releases increment the patch component. Non-stable tags and pre-release tags are ignored when choosing the next version.

The source tree deliberately uses `0.0.0-dev` for local builds. The Release workflow passes its resolved version to the build and packaging commands so the application, installer, portable archive, checksums, tag, and GitHub release all agree.

If a run fails after creating its tag or release, fix the underlying infrastructure problem and rerun the same workflow. A stable tag already pointing at that `main` commit is reused, and existing release assets are replaced safely. Tags on any other commit are never moved or overwritten.

After a successful release, merge `main` back into `dev` if GitHub reports that the branches have diverged. This keeps the exact released commit in the development history.

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
