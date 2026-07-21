# Testing

Run the complete deterministic test suite from the repository root with one command:

```powershell
dotnet test Unskip.sln
```

Tests must not contact or send messages to real network devices. Future real `msg.exe` integration tests must be explicitly configured, opt-in, and disabled by default.
