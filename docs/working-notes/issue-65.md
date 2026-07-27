# Issue 65: automatic release versioning

- `main` remains the only branch that publishes GitHub releases.
- Stable release versions are resolved from existing `vMAJOR.MINOR.PATCH` tags.
- Pre-1.0 releases increment the minor component; releases from 1.0 onward increment the patch component.
- Re-running a release for an already tagged commit reuses that tag and safely replaces release assets.
- Local builds identify themselves as `0.0.0-dev`; release builds receive one resolved version across compilation and packaging.
- The resolver is exercised in CI against isolated temporary Git repositories.
