# Message composer

The Send workspace opens only after a saved or one-time destination has been resolved. It keeps the friendly alias and actual hostname or IPv4 address visible while the user writes the message.

Messages are limited to 1,024 characters. The send action is asynchronous and disabled while a request is in progress. Failed and timed-out requests keep the draft and expose a retry action. Sanitized native output is hidden behind an optional technical-details control.

`Sent` means that Windows accepted the native request. It does not prove that a recipient saw, read, or acknowledged the message.

The native delivery boundary accepts validated hostnames and canonical dotted-decimal IPv4 destinations. A valid destination can still be rejected by Windows because of reachability, session state, permissions, or policy.
