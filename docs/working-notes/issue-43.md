# Issue 43 working note

- Kept canonical IPv4 addresses available in the device directory and composer.
- Added an injected DNS boundary for deterministic, network-free tests.
- Required reverse DNS results to resolve forward to the requested IPv4 address.
- Passed only the verified computer name to the independent `/SERVER:` argument.
- Returned a retryable target-unavailable result without starting `msg.exe` when DNS verification failed.
- Preserved direct shell-free process execution and message-body sanitization.
