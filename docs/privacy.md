# Privacy

Unskip is local-first. It has no central server, cloud backend, analytics, or telemetry. Issue #2 stores no device directory, message history, or message bodies.

Later persistence will belong to the current Windows user under local application data. Runtime databases, exports, logs containing user data, and personal network details must never be committed.

Windows native messaging cannot prove that a recipient read, displayed, or acknowledged a message. The interface and history must not imply otherwise. A later issue will document the final policy for message-body retention before any body is stored.
