# Local send history

Unskip records completed send attempts only in the current Windows user's SQLite database. Each entry contains immutable snapshots of the alias, computer name, IPv4 address, selected destination, timestamp, result status, failure category, duration, exit code, message length, and a bounded sanitized diagnostic summary.

Message bodies are never stored. Reusing an entry restores its destination with an empty composer, so the user must intentionally write the message again. This trades one-click replay for safer local data retention.

History can be searched by alias or destination and filtered by sent, rejected, timed-out, or failed status. Individual entries and the full local history can be deleted only after explicit confirmation. Nothing is uploaded or synchronized.

`Sent` records that Windows accepted the request. It is not proof that a person saw, read, or acknowledged the message.
