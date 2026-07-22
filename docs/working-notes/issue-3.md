# Issue #3 implementation plan

1. Define message request, target, validation, status, failure, and sender contracts in core.
2. Implement direct `msg.exe` invocation with independent arguments and no shell.
3. Add bounded timeout, cancellation, process-tree termination, captured diagnostics, sanitization, and honest result mapping.
4. Verify validation and process behavior with deterministic unit tests and a local non-network test process.
5. Document Windows prerequisites, hostname-only support, result semantics, security boundaries, and opt-in real integration expectations.
