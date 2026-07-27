[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$Commit = 'HEAD'
)

$ErrorActionPreference = 'Stop'
$stableTagPattern = '^v(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)$'

function Invoke-RepositoryGit {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = @(& git -C $RepositoryRoot @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed: $($output -join [Environment]::NewLine)"
    }

    return $output
}

function ConvertTo-StableRelease {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag
    )

    if ($Tag -notmatch $stableTagPattern) {
        return $null
    }

    return [pscustomobject]@{
        Tag = $Tag
        Major = [int]$Matches.major
        Minor = [int]$Matches.minor
        Patch = [int]$Matches.patch
    }
}

function Select-LatestRelease {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Releases
    )

    return $Releases |
        Sort-Object -Property `
            @{ Expression = 'Major'; Descending = $true }, `
            @{ Expression = 'Minor'; Descending = $true }, `
            @{ Expression = 'Patch'; Descending = $true } |
        Select-Object -First 1
}

$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$resolvedCommit = ((Invoke-RepositoryGit -Arguments @('rev-parse', '--verify', "$Commit`^{commit}")) -join '').Trim()

$tagsAtCommit = Invoke-RepositoryGit -Arguments @('tag', '--points-at', $resolvedCommit)
$releaseAtCommit = @(
    $tagsAtCommit |
        ForEach-Object { ConvertTo-StableRelease -Tag $_ } |
        Where-Object { $null -ne $_ }
)

if ($releaseAtCommit.Count -gt 0) {
    $existingRelease = Select-LatestRelease -Releases $releaseAtCommit
    return [pscustomobject]@{
        Version = "$($existingRelease.Major).$($existingRelease.Minor).$($existingRelease.Patch)"
        Tag = $existingRelease.Tag
        TagExists = $true
        Commit = $resolvedCommit
    }
}

$allTags = Invoke-RepositoryGit -Arguments @('tag', '--list')
$stableReleases = @(
    $allTags |
        ForEach-Object { ConvertTo-StableRelease -Tag $_ } |
        Where-Object { $null -ne $_ }
)

if ($stableReleases.Count -eq 0) {
    $nextVersion = '0.1.0'
} else {
    $latestRelease = Select-LatestRelease -Releases $stableReleases
    if ($latestRelease.Major -eq 0) {
        $nextVersion = "0.$($latestRelease.Minor + 1).0"
    } else {
        $nextVersion = "$($latestRelease.Major).$($latestRelease.Minor).$($latestRelease.Patch + 1)"
    }
}

return [pscustomobject]@{
    Version = $nextVersion
    Tag = "v$nextVersion"
    TagExists = $false
    Commit = $resolvedCommit
}
