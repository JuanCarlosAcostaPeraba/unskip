## Summary

Describe what changed and why.

Closes #

## Validation

- [ ] `dotnet restore Unskip.sln`
- [ ] `dotnet build Unskip.sln --no-restore`
- [ ] `dotnet test Unskip.sln --no-build`
- [ ] `dotnet format Unskip.sln --verify-no-changes --no-restore`
- [ ] Documentation was updated where needed
- [ ] The diff contains no secrets, personal data, real network details, generated binaries, or local databases

## Product invariants

- [ ] No server, cloud, telemetry, sound, Node.js, Electron, or embedded browser was introduced
- [ ] No read/displayed/acknowledged delivery claim was introduced
- [ ] Any process execution avoids shells and passes arguments separately
