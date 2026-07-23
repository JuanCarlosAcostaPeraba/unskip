# Native message delivery

## Supported behavior

Unskip uses the Windows `msg.exe` program directly. It sends to all active sessions on a validated hostname or canonical IPv4 destination by passing exactly three independent arguments:

1. `*`
2. `/SERVER:<validated-destination>`
3. the message body

No command shell, script host, string-built command, `/W`, file input, or standard-input message body is used. Quotes and shell-like characters in the body remain part of the single message argument.

The maximum message length is 1,024 UTF-16 characters. Newlines and tabs are allowed; other control characters are rejected.

## Destination support

Windows documents `/SERVER` as accepting a server name. Unskip accepts ASCII computer or DNS names and canonical dotted-decimal IPv4 addresses, then passes the validated value as one independent argument without shell interpretation. Ambiguous IPv4 forms such as shortened notation or segments with leading zeroes are rejected. Windows remains authoritative for whether a particular destination is reachable and permitted.

## Result semantics

- `Sent`: `msg.exe` exited with code zero. It does not prove that a person read, saw, or acknowledged the message.
- `Rejected`: validation or Windows rejected the request.
- `TimedOut`: the native process exceeded the configured execution timeout and was terminated.
- `Cancelled`: the caller cancelled sending and the native process was terminated if it had started.
- `Failed`: the executable could not start, process execution failed, or safe termination failed.

Standard output and error are captured for support diagnostics. Message-body occurrences are removed, control characters are sanitized, and each diagnostic is limited to 2,048 characters. Unskip does not log message bodies by default.

## Windows prerequisites

- A supported Windows version with `msg.exe` available.
- A target hostname or canonical IPv4 address reachable through the Windows facilities used by `msg.exe`.
- An active compatible user session on the target.
- Message special access permission for the sender, as required by Windows.
- Domain, workgroup, local security, network, and firewall policy that permits the native operation for the specific sender and target.

Unskip does not change firewall rules, registry values, Windows services, session permissions, or organizational policy. Ping success is not proof that native delivery will work. Troubleshooting must not recommend disabling protections or enabling broad access as a generic fix.

## Real integration verification

Real-network verification must use explicitly designated test computers and fictitious message content. It must be manually opted into, never run in ordinary unit tests or CI, and never embed real computer names, addresses, credentials, or infrastructure details in source control.
