# Issue 6 working notes

- Added an MVVM message composer connected to `IMessageSender`.
- Preserved alias and resolved technical destination during confirmation.
- Added message length feedback, validation, in-progress state, duplicate-send protection, honest result copy, optional technical details, and retry for failed or timed-out sends.
- Kept drafts intact after unsuccessful delivery.
- Added deterministic view-model tests with fake senders; no automated test contacts a network device.
- Kept IPv4 delivery rejected in accordance with the existing secure sender boundary from issue 3.
- Visually exercised the Release build and completed one explicitly confirmed hostname delivery in a controlled Windows environment. Windows returned `Sent`; no real hostname, IP address, or message body is recorded here.
