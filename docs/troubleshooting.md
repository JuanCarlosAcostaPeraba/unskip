# Troubleshooting

Start with the result and **Technical details** shown by Unskip. Diagnostics are sanitized and bounded, and message-body occurrences are removed. Do not post real computer names, addresses, user identities, or message content in a public issue.

## Windows shows a SmartScreen warning

Early Unskip releases are unsigned, so Windows may identify the installer or executable as an unknown publisher or unrecognized app. Managed policy may block unsigned software completely.

Download only from the repository's GitHub Releases page and compare the file against `SHA256SUMS.txt`. Follow your organization's policy and ask the responsible administrator if execution is blocked. Do not disable SmartScreen, antivirus, or organizational controls as a workaround.

## The installer cannot update files

Close Unskip and run the installer again. The installer targets only the current Windows account and normally needs no administrator rights. An upgrade reuses `%LOCALAPPDATA%\Programs\Unskip` and leaves `%LOCALAPPDATA%\Unskip` untouched.

If policy blocks installation, use the portable ZIP only when organizational policy permits it. Do not move the local database into the application directory.

## Update check or download fails

Normal messaging and saved devices remain available offline. Confirm that the computer can reach `api.github.com` and `github.com` over HTTPS, then use **Check for updates** again. A firewall, proxy, GitHub outage, rate limit, or organizational policy can prevent the optional check.

Unskip rejects releases with an unexpected tag, filename, repository URL, size, or SHA-256 checksum. Do not rename an installer into the update cache or bypass verification. Download only from this repository's GitHub Releases page if the in-app flow remains unavailable.

If **Install update** reports that the downloaded file changed or is damaged, close Unskip, remove `%LOCALAPPDATA%\Unskip\updates`, reopen it, and download again. This removes only cached installers, not `unskip.db`.

## The required SDK is not found

This applies only to source builds. Install a stable .NET 10 SDK and run `dotnet --info`. The repository's `global.json` rejects preview SDKs and rolls forward within stable .NET 10 feature bands.

## Restore fails

Confirm that the development machine can reach NuGet.org and that its NuGet configuration is readable. Runtime use of Unskip does not require Internet access; package restore is a development operation.

## WPF does not build

Build on Windows 10 or Windows 11 with the .NET 10 SDK. WPF is Windows-only. If using Visual Studio, install the .NET desktop development workload.

## IPv4 is rejected before sending

This is expected. The directory can retain a canonical IPv4 address, but the current native boundary supports Windows computer names or DNS names only. Select or enter the computer name. Do not work around the validation by editing the database.

## Windows reports permission denied or error 5

Unskip maps exit code 5, or a bounded diagnostic containing error 5, to **Permission denied**. The sender does not currently have the Windows permission required to message sessions on that destination.

Verify that the destination is correct and ask the responsible administrator to review the sender's message-session permission for that specific computer. Do not use unrestricted administrator rights as a permanent workaround, and do not relax policy for every user or computer.

## Windows cannot contact the target: errors 53, 1722, or 1726

Unskip maps these verified native codes to **Target unavailable**. Check:

1. the Windows computer name is spelled correctly;
2. the name resolves from the sending computer;
3. the target is powered on and reachable through the managed network;
4. a compatible Windows session is active;
5. the responsible administrator permits the required native Windows operation between these specific computers.

Ping success does not prove that the remote session operation is available. Do not disable Windows Firewall, expose broad port ranges, enable services globally, or edit registry and domain policy as a generic fix. Ask an administrator for the narrowest change that matches organizational policy.

## Windows rejected the request with another code

Unskip reports **Native rejected** when `msg.exe` returns a non-zero result that is not one of the verified mappings above. Expand **Technical details**, note the sanitized exit code and diagnostic, and compare it with the destination's Windows/session prerequisites. Avoid guessing from undocumented codes.

## The request timed out

Unskip stops the native process when it exceeds the configured timeout. Confirm the target name and session state, then retry once. Repeated timeouts should be investigated as a target, network, service, or policy problem; increasing timeouts or weakening network controls is not the default remedy.

## `msg.exe` is unavailable

Unskip reports this when Windows cannot find the native executable. Confirm that the sending computer is a supported Windows installation and that `%WINDIR%\System32\msg.exe` exists. Do not download a replacement executable from an unofficial site.

## Unskip says Sent but the recipient did not respond

`Sent` means only that Windows returned exit code zero. It does not prove display, reading, attention, or acknowledgement. Unskip does not produce sound, read receipts, or presence information. Confirm the intended computer and session through an appropriate separate channel.

## The local database cannot be opened

Confirm that the current Windows account can write to `%LOCALAPPDATA%\Unskip`. Close Unskip before copying or moving `unskip.db`, `unskip.db-shm`, or `unskip.db-wal`.

Renaming the entire `Unskip` directory while the application is closed is a recoverable way to let Unskip create a fresh database while preserving the original for investigation. Do not edit the SQLite file manually.

## Back up, restore, or reset local data

Close Unskip and copy the complete `%LOCALAPPDATA%\Unskip` folder, including SQLite sidecar files. Restore the complete folder only while Unskip is closed.

Deleting the folder removes saved devices and history and cannot be undone without a backup. Clearing history from the UI leaves saved devices intact. Removing the application files does not necessarily remove local data.

## A saved device does not appear in search

Clear the search box or search by alias, computer name, IPv4 address, or description. Search is case-insensitive. Favorites and recently used devices appear first when no filter is active.

## Reporting a problem

For ordinary bugs, follow the repository issue template and replace every real hostname, address, alias, and message with fictitious data. For security vulnerabilities, follow [SECURITY.md](../SECURITY.md) and do not open a public issue containing undisclosed details.
