[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$resolver = Join-Path $PSScriptRoot 'resolve-release-version.ps1'
$temporaryBase = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$temporaryRepository = Join-Path $temporaryBase "unskip-release-version-$([guid]::NewGuid().ToString('N'))"

function Invoke-TestGit {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = @(& git -C $temporaryRepository @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return $output
}

function New-TestCommit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Content
    )

    Set-Content -LiteralPath (Join-Path $temporaryRepository 'release.txt') -Value $Content -Encoding utf8
    Invoke-TestGit -Arguments @('add', 'release.txt') | Out-Null
    Invoke-TestGit -Arguments @('commit', '--quiet', '-m', "Commit $Content") | Out-Null
}

function Assert-Resolution {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion,
        [Parameter(Mandatory = $true)]
        [bool]$ExpectedTagExists
    )

    $result = & $resolver -RepositoryRoot $temporaryRepository
    if ($result.Version -ne $ExpectedVersion -or $result.TagExists -ne $ExpectedTagExists) {
        throw "Expected version $ExpectedVersion (tag exists: $ExpectedTagExists), received $($result.Version) (tag exists: $($result.TagExists))."
    }
}

try {
    New-Item -ItemType Directory -Path $temporaryRepository | Out-Null
    Invoke-TestGit -Arguments @('init', '--quiet') | Out-Null
    Invoke-TestGit -Arguments @('config', 'user.name', 'Unskip Release Test') | Out-Null
    Invoke-TestGit -Arguments @('config', 'user.email', 'release-test@unskip.invalid') | Out-Null

    New-TestCommit -Content 'initial'
    Assert-Resolution -ExpectedVersion '0.1.0' -ExpectedTagExists $false

    Invoke-TestGit -Arguments @('tag', 'v0.3.0') | Out-Null
    Assert-Resolution -ExpectedVersion '0.3.0' -ExpectedTagExists $true

    New-TestCommit -Content 'next minor'
    Invoke-TestGit -Arguments @('tag', 'latest') | Out-Null
    Invoke-TestGit -Arguments @('tag', 'v0.4.0-beta.1') | Out-Null
    Assert-Resolution -ExpectedVersion '0.4.0' -ExpectedTagExists $false

    Invoke-TestGit -Arguments @('tag', 'v0.4.0') | Out-Null
    Assert-Resolution -ExpectedVersion '0.4.0' -ExpectedTagExists $true

    New-TestCommit -Content 'stable patch'
    Invoke-TestGit -Arguments @('tag', 'v1.2.3', 'HEAD~1') | Out-Null
    Assert-Resolution -ExpectedVersion '1.2.4' -ExpectedTagExists $false

    Write-Host 'Release version resolver tests passed.'
} finally {
    $resolvedTemporaryRepository = [System.IO.Path]::GetFullPath($temporaryRepository)
    $temporaryPrefix = $temporaryBase.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar

    if (
        (Test-Path -LiteralPath $resolvedTemporaryRepository) -and
        $resolvedTemporaryRepository.StartsWith($temporaryPrefix, [System.StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolvedTemporaryRepository).StartsWith('unskip-release-version-', [System.StringComparison]::Ordinal)
    ) {
        Remove-Item -LiteralPath $resolvedTemporaryRepository -Recurse -Force
    }
}
