namespace Unskip.Core.Updates;

public sealed record ApplicationUpdateRelease(
    SemanticVersion Version,
    string TagName,
    string InstallerFileName,
    Uri InstallerUri,
    long InstallerSize,
    Uri ChecksumUri);
