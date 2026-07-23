# Issue #33 working notes

- Fixed the mismatch between the device directory, which supported canonical IPv4 destinations, and message delivery, which rejected them before process execution.
- Preserved explicit target typing so hostnames and IPv4 addresses remain distinguishable after validation.
- Kept `msg.exe` execution direct, shell-free, and separated into independent `ArgumentList` entries.
- Rejected ambiguous IPv4 notation while accepting canonical dotted-decimal values.
- Used only fictitious documentation addresses in code, tests, and documentation.
