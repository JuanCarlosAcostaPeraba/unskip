# Issue 21 working notes

- Created `dev` from the `main` commit containing the Windows MVP packaging work.
- Feature and dependency pull requests target `dev`; release promotions target `main`.
- CI validates `dev` and pull requests, while the release workflow performs the full validation again for `main`.
- The repository version is the release authority. Merging a version already tagged on another commit fails before packaging.
- The workflow creates its annotated tag only after validation and packaging succeed.
- Reruns accept the same tag on the same commit and replace existing release assets when necessary.
- Signing, automatic version selection, and WinGet publication remain out of scope.
