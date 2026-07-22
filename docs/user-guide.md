# User guide

Unskip is an unofficial Windows desktop application that submits native session messages to another Windows computer on an accessible local network. It is local-first: there is no Unskip server, cloud account, telemetry service, recipient-side Unskip installation, or Internet requirement at runtime.

Unskip is not a chat platform, guaranteed-delivery system, monitoring tool, or acknowledgement service. It does not play a sound. A `Sent` result means only that Windows accepted the native request; it does not prove that anyone saw, read, or acknowledged the message.

## Current installation status

Unskip is currently a pre-release project. A supported installer or signed end-user package has not been published yet. Issue #10 tracks packaging and release work.

To evaluate the current source build, use a Windows 10 or Windows 11 computer with the stable .NET 10 SDK:

```powershell
dotnet restore Unskip.sln
dotnet build Unskip.sln --configuration Release --no-restore
dotnet run --project src/Unskip.App/Unskip.App.csproj --configuration Release
```

These terminal commands are for contributors and pre-release evaluation only. The application itself never invokes PowerShell, `cmd.exe`, or another shell. When packaged releases exist, use only the installer and checksums published by this repository.

## Before the first message

The sending computer needs:

- Windows 10 or Windows 11 with `msg.exe` available;
- the Windows computer name or DNS name of the destination;
- network access to the Windows facilities used by `msg.exe`;
- permission to message sessions on the destination;
- an active compatible session on the destination.

The recipient does not install Unskip or create an Unskip account. Organizational Windows policy, session configuration, and network controls can still prevent native messaging. Ping success alone does not prove that delivery will work.

Unskip currently sends only to computer names or DNS names. IPv4 addresses such as `192.168.50.25` can be saved in the directory, but the composer rejects them before starting `msg.exe`.

### Windows environment checklist

- **Computer name:** the sender must resolve the intended Windows computer name through the managed network.
- **Identity and domain policy:** the current Windows identity must be allowed to message sessions on that specific destination. Domain, workgroup, and local policy can affect that permission.
- **Session:** a compatible target session must be active; Unskip does not create, sign in to, or wake sessions.
- **Network and firewall:** managed controls must permit the native Windows operation between the sender and destination.
- **Administration:** if a prerequisite is blocked, ask the responsible administrator to verify the narrow sender/target path. Do not disable the firewall, expose broad ports, enable services globally, or change registry/domain policy as a generic workaround.

## Send the first message

1. Open Unskip and choose **Devices**.
2. Select **Add device**, or enter a one-time computer name such as `lab-pc-01`.
3. For a saved device, enter a friendly alias and at least one technical destination. Choose the computer name as the preferred destination when sending.
4. Select the device or choose **Use** for the one-time destination.
5. Confirm the alias and resolved technical destination shown under **Resolved destination**.
6. Choose **Prepare message**.
7. Write a message of at most 1,024 characters and choose **Send message**.
8. Read the result shown by Unskip. Open **Technical details** only when troubleshooting.

The send button is disabled while a request is active. A failed or timed-out request keeps the draft and offers **Retry with this draft**.

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

The same hostname validation applies to saved and one-time destinations. Only letters, digits, hyphens, and dots are accepted for message delivery.

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

Unskip relies on Windows account and filesystem permissions. It does not currently add application-level encryption to the database.

## Important limitations

- Windows only.
- No sound or attention tone.
- No read receipts, acknowledgements, guaranteed delivery, queued offline delivery, or recipient presence indicator.
- No central server, cloud synchronization, account system, or telemetry.
- No recipient-side Unskip installation.
- Hostname delivery only; IPv4 delivery is deliberately disabled.
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
