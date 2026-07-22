[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$NsisCompiler
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repositoryRoot 'artifacts'
$publishDirectory = Join-Path $artifactsRoot 'publish\win-x64'
$releaseDirectory = Join-Path $artifactsRoot 'release'
$project = Join-Path $repositoryRoot 'src\Unskip.App\Unskip.App.csproj'
$installerScript = Join-Path $repositoryRoot 'installer\Unskip.nsi'

function Reset-ArtifactDirectory([string]$Path) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $fullPath.StartsWith($fullArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to reset a directory outside the repository artifacts folder: $fullPath"
    }

    if (Test-Path -LiteralPath $fullPath) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $fullPath | Out-Null
}

function Find-NsisCompiler {
    if ($NsisCompiler) {
        return $NsisCompiler
    }

    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'NSIS\makensis.exe'),
        (Join-Path $env:ProgramFiles 'NSIS\makensis.exe')
    )

    return $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

Reset-ArtifactDirectory $publishDirectory
Reset-ArtifactDirectory $releaseDirectory

$commit = (git -C $repositoryRoot rev-parse --short=12 HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to determine the source commit.'
}

dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:InformationalVersion="$Version+$commit" `
    -p:IncludeSourceRevisionInInformationalVersion=false
if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

$portableArchive = Join-Path $releaseDirectory "Unskip-$Version-win-x64.zip"
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $portableArchive -CompressionLevel Optimal

$compiler = Find-NsisCompiler
if (-not $compiler -or -not (Test-Path -LiteralPath $compiler)) {
    throw 'NSIS compiler was not found. Install NSIS 3.12 or pass -NsisCompiler.'
}

$numericVersion = "$($Version.Split('-', 2)[0]).0"
& $compiler `
    '/WX' `
    "/DAPP_VERSION=$Version" `
    "/DNUMERIC_VERSION=$numericVersion" `
    "/DSOURCE_DIR=$publishDirectory" `
    "/DOUTPUT_DIR=$releaseDirectory" `
    $installerScript
if ($LASTEXITCODE -ne 0) {
    throw 'NSIS compilation failed.'
}

$checksumFile = Join-Path $releaseDirectory 'SHA256SUMS.txt'
$checksumLines = Get-ChildItem -LiteralPath $releaseDirectory -File |
    Where-Object { $_.Name -ne 'SHA256SUMS.txt' } |
    Sort-Object Name |
    ForEach-Object {
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($_.Name)"
    }
$checksumLines | Set-Content -LiteralPath $checksumFile -Encoding ascii

Get-ChildItem -LiteralPath $releaseDirectory -File | Sort-Object Name | Select-Object Name, Length
