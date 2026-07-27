# Message composer

The Send workspace opens only after a saved or one-time destination has been resolved. Its minimal two-column layout keeps the draft dominant while the friendly alias, actual hostname or IPv4 address, and honest result remain visible in one compact context card.

Messages are limited to 1,024 characters. The send action is asynchronous and disabled while a request is in progress. Failed and timed-out requests keep the draft and expose a retry action. Sanitized native output is hidden behind an optional technical-details control.

The composer also exposes an explicit **Preview** action for the urgent-attention overlay prototype. The preview renders the current validated draft in memory, covers the current Windows virtual screen, and keeps the message card wholly on the primary monitor instead of centering it across a physical monitor seam. The card shows only the local source status, the draft as its dominant large content, and the fixed close button. It does not send, queue, or persist any message data. Empty, oversized, and otherwise invalid drafts cannot be previewed. The overlay closes through its fixed button, Escape, Alt+F4, or a one-minute safety timeout. Sending and navigation remain disabled until the preview closes.

`Sent` means that Windows accepted the native request. It does not prove that a recipient saw, read, or acknowledged the message.

The native delivery boundary accepts validated hostnames and canonical dotted-decimal IPv4 destinations. IPv4 destinations must resolve to a forward-verified computer name before Windows is contacted. A valid and verified destination can still be rejected by Windows because of reachability, session state, permissions, or policy.
