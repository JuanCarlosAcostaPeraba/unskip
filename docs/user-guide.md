# User guide

Unskip is an unofficial Windows desktop application that submits native session messages to another Windows computer on an accessible local network. It is local-first: there is no Unskip server, cloud account, telemetry service, recipient-side Unskip installation, or Internet requirement at runtime.

Unskip is not a chat platform, guaranteed-delivery system, monitoring tool, or acknowledgement service. It does not play a sound. A `Sent` result means only that Windows accepted the native request; it does not prove that anyone saw, read, or acknowledged the message.

## Install a release

Download releases only from the repository's [GitHub Releases page](https://github.com/JuanCarlosAcostaPeraba/unskip/releases). Each release provides:

- `Unskip-VERSION-win-x64-setup.exe`, the recommended per-user installer;
- `Unskip-VERSION-win-x64.zip`, a portable copy for evaluation;
- `SHA256SUMS.txt`, the integrity checksums for both artifacts.

Compare the downloaded file's SHA-256 hash with `SHA256SUMS.txt` before running it. The installer and portable archive are self-contained and do not require a separate .NET installation. They support x64 Windows 10 and Windows 11.

The installer uses the current Windows account and does not request administrator rights by default. It installs under `%LOCALAPPDATA%\Programs\Unskip`, creates a Start menu shortcut, and offers an optional desktop shortcut. Use Windows **Installed apps** to uninstall it.

Early releases are not code-signed because the project does not have a verified signing certificate. [Microsoft Defender SmartScreen](https://learn.microsoft.com/windows/apps/package-and-deploy/smartscreen-reputation) can therefore show an unknown-publisher or unrecognized-app warning, and managed devices may block execution. Verify the GitHub repository and checksum, then follow your organization's security policy. Do not disable SmartScreen or other protections to install Unskip.

For a portable run, extract the complete ZIP to a normal user-writable folder and launch `Unskip.App.exe`. Do not run it from inside the ZIP.

## Update Unskip

The sidebar shows the installed version and a **Check for updates** button. Unskip contacts the official GitHub Releases API only after that button is selected; it does not check in the background and does not require an account or token.

The sidebar also identifies the developer, Juan Carlos Acosta Perabá. Selecting the name opens [jcap.tech](https://jcap.tech) in the default browser.

If a newer stable release exists:

1. select **Download update**;
2. wait while Unskip downloads the Windows x64 installer and `SHA256SUMS.txt`;
3. Unskip enables **Install update** only after the installer filename, source URL, size, and SHA-256 checksum are verified;
4. select **Install update** to start the installer directly and close Unskip;
5. complete the visible installer. Existing devices and history remain under `%LOCALAPPDATA%\Unskip`.

The update cache is stored below `%LOCALAPPDATA%\Unskip\updates`. It can be removed while Unskip is closed without deleting the device directory or history database.

Updates are optional and user-initiated. A failed or offline check does not block normal use. Early installers remain unsigned and Windows may still show a SmartScreen warning; follow the guidance above rather than disabling security controls.

## Evaluate from source

To evaluate the current source build, use a Windows 10 or Windows 11 computer with the stable .NET 10 SDK:

```powershell
dotnet restore Unskip.sln
dotnet build Unskip.sln --configuration Release --no-restore
dotnet run --project src/Unskip.App/Unskip.App.csproj --configuration Release
```

These terminal commands are for contributors and source evaluation only. The application itself never invokes PowerShell, `cmd.exe`, or another shell.

## Before the first message

The sending computer needs:

- Windows 10 or Windows 11 with `msg.exe` available;
- the Windows computer name, DNS name, or canonical IPv4 address of the destination;
- network access to the Windows facilities used by `msg.exe`;
- permission to message sessions on the destination;
- an active compatible session on the destination.

The recipient does not install Unskip or create an Unskip account. Organizational Windows policy, session configuration, and network controls can still prevent native messaging. Ping success alone does not prove that delivery will work.

Unskip accepts computer names, DNS names, and canonical dotted-decimal IPv4 addresses. Windows can still reject a valid destination because of reachability, session state, permissions, or organizational policy.

### Windows environment checklist

- **Computer name:** the sender must resolve the intended Windows computer name through the managed network.
- **Identity and domain policy:** the current Windows identity must be allowed to message sessions on that specific destination. Domain, workgroup, and local policy can affect that permission.
- **Session:** a compatible target session must be active; Unskip does not create, sign in to, or wake sessions.
- **Network and firewall:** managed controls must permit the native Windows operation between the sender and destination.
- **Administration:** if a prerequisite is blocked, ask the responsible administrator to verify the narrow sender/target path. Do not disable the firewall, expose broad ports, enable services globally, or change registry/domain policy as a generic workaround.

## Send the first message

1. Open Unskip and choose **Devices**.
2. Select **Add device**, or enter a one-time computer name such as `lab-pc-01`.
3. For a saved device, enter a friendly alias and at least one technical destination. Choose the hostname or canonical IPv4 address that should be used for sending.
4. Select the device or choose **Use** for the one-time destination.
5. Confirm the alias and resolved technical destination shown under **Resolved destination**.
6. Choose **Prepare message**.
7. Write a message of at most 1,024 characters and choose **Send message**.
8. Read the result shown by Unskip. Open **Technical details** only when troubleshooting.

The send button is disabled while a request is active. A failed or timed-out request keeps the draft and offers **Retry with this draft**.

### Preview the urgent-attention design locally

Choose **Preview** in the message composer to inspect how the current validated draft would look in the proposed full-screen presentation on this computer. It does not contact the selected destination, write the draft to history or another local store, or prove that anyone read anything. Empty, oversized, and otherwise invalid drafts keep the preview action disabled.

The preview dims the complete Windows virtual screen and keeps its message-first card wholly on the primary monitor so it is not split between displays. The card contains only the local source status, the large message, and the fixed **Close message** button. Escape, Alt+F4, and the one-minute safety timeout remain available. A display-topology change also closes it safely. Remote transport, recipient installation, firewall configuration, background startup, and policy-controlled deployment are deliberately outside this prototype.

## Saved devices

A saved device can contain:

- a friendly alias;
- a Windows computer name;
- a canonical IPv4 address;
- an optional description;
- a preferred destination;
- a favorite flag.

At least one technical destination is required. Search covers alias, computer name, IPv4 address, and description. Favorites and recently used devices appear first when no search is active.

Editing or deleting a device does not rewrite old history entries. Deleting a device requires confirmation.

## One-time destinations

Use **One-time destination** when a target should not be retained. The resolved value is displayed before the composer opens. **Save as device** converts it into a normal directory entry.

The same destination validation applies to saved and one-time destinations. Hostnames use ASCII letters, digits, hyphens, and dots. IPv4 addresses must contain four dotted-decimal segments without leading zeroes.

## Local history

**History** lists completed attempts and can filter sent, rejected, timed-out, and failed results. It stores destination and result snapshots, but never the message body.

**Use destination** opens an empty composer for the historical target. You must write a new message intentionally. Individual entries and the full history can be deleted after confirmation.

## Result meanings

| Result | Meaning |
| --- | --- |
| Sent | Windows returned exit code zero. This is not proof of reading, display, or acknowledgement. |
| Rejected | Validation or Windows rejected the request. |
| Timed out | The native process exceeded the configured timeout and Unskip stopped it. |
| Cancelled | Sending was cancelled before completion. |
| Failed | The native process could not start, failed unexpectedly, or could not be stopped safely. |

## Local data, backup, and deletion

The current Windows user's data is stored in:

```text
%LOCALAPPDATA%\Unskip\
```

The folder contains `unskip.db` and may contain SQLite `-wal` or `-shm` sidecar files. It can include aliases, destinations, descriptions, timestamps, favorites, result metadata, and sanitized diagnostics. It contains no message bodies or credentials, but destination information may still be sensitive.

To back up or restore data:

1. Close Unskip completely.
2. Copy or restore the entire `%LOCALAPPDATA%\Unskip` folder, including sidecar files.
3. Keep backups protected with the same care as the Windows account.

Deleting the application files does not necessarily delete this folder. Deleting the folder removes the saved directory and history; the next launch creates a new database. This is irreversible without a backup. Clearing history in the UI does not delete saved devices.

Installing a newer version or uninstalling Unskip leaves this folder untouched. This preserves devices and history across upgrades and makes uninstall non-destructive. Delete the folder separately only when you intentionally want to erase the current account's Unskip data.

Unskip relies on Windows account and filesystem permissions. It does not currently add application-level encryption to the database.

## Important limitations

- Windows only.
- No sound or attention tone.
- No read receipts, acknowledgements, guaranteed delivery, queued offline delivery, or recipient presence indicator.
- No central server, cloud synchronization, account system, or telemetry.
- No recipient-side Unskip installation.
- Hostname and canonical IPv4 delivery depend on the native Windows environment.
- Native messaging can be blocked by Windows edition, session state, permissions, services, network controls, or organizational policy.
- Technical details are sanitized and bounded, but should still be treated as support data.

Unskip never changes firewall rules, registry settings, services, session permissions, or organizational policy. Do not weaken those controls broadly to make messaging work. See [Troubleshooting](troubleshooting.md) for safe, scoped checks.

## More information

- [Troubleshooting](troubleshooting.md)
- [Privacy](privacy.md)
- [Local persistence](local-persistence.md)
- [Security design](security.md)
- [Threat model](threat-model.md)
- [Native delivery details](message-delivery.md)

Unskip is an independent community project. It is not affiliated with, endorsed by, or sponsored by Microsoft or any employer.
