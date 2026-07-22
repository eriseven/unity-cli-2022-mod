<#
.SYNOPSIS
    Unity CLI Installer for Windows.

.DESCRIPTION
    Downloads and installs the Unity CLI binary for Windows.
    Verifies the download with SHA-256 before installing.

.EXAMPLE
    irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex

.PARAMETER Target
    Version to install. Defaults to "latest" (recommended).

.LINK
    https://unity.com
#>

param(
    [string]$Target = "latest",
    [ValidateSet("", "alpha", "beta")]
    [string]$Channel = $env:UNITY_CLI_CHANNEL
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"  # Faster downloads

$CdnBase   = "https://public-cdn.cloud.unity3d.com/hub/prod/cli/"
$InstallDir = Join-Path $env:LOCALAPPDATA "Unity\bin"

# Channel selects the version manifest: latest.json, latest-beta.json, latest-alpha.json
$ManifestFile = switch ($Channel) {
    "alpha" { "latest-alpha.json" }
    "beta"  { "latest-beta.json" }
    default { "latest.json" }
}

# ─── Platform check ───────────────────────────────────────────────────────────

if ([System.IntPtr]::Size -ne 8) {
    Write-Host "`n  error Windows 32-bit is not supported. Unity CLI requires a 64-bit system.`n" -ForegroundColor Red
    exit 1
}

$arch = if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { "arm64" } else { "x64" }
$platformKey = "win32-$arch"

# ─── Output helpers ───────────────────────────────────────────────────────────

function Write-Info($msg) {
    Write-Host "  " -NoNewline
    Write-Host "info" -ForegroundColor Cyan -NoNewline
    Write-Host "  $msg"
}

function Write-Warn($msg) {
    Write-Host "  " -NoNewline
    Write-Host "warn" -ForegroundColor Yellow -NoNewline
    Write-Host "  $msg"
}

function Fail($msg) {
    Write-Host "`n  " -NoNewline
    Write-Host "error" -ForegroundColor Red -NoNewline
    Write-Host " $msg`n"
    exit 1
}

# ─── Fetch manifest ───────────────────────────────────────────────────────────

function Get-Manifest {
    $url = "${CdnBase}${ManifestFile}"
    Write-Info "Checking for latest version..."
    try {
        $iwrParams = @{ Uri = $url; ErrorAction = "Stop" }
        if ($PSVersionTable.PSVersion.Major -lt 6) {
            $iwrParams["UseBasicParsing"] = $true
        }
        $response = Invoke-WebRequest @iwrParams
        return $response.Content | ConvertFrom-Json
    }
    catch {
        Fail "Failed to fetch version manifest from $url`n  $_"
    }
}

# ─── Download binary ──────────────────────────────────────────────────────────

function Get-Binary($url, $dest) {
    Write-Info "Downloading binary..."
    try {
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $dest)
    }
    catch {
        Fail "Failed to download binary from $url`n  $_"
    }
}

# ─── SHA-256 verification ─────────────────────────────────────────────────────

function Confirm-Sha256($file, $expected) {
    Write-Info "Verifying checksum..."
    $actual = (Get-FileHash -Path $file -Algorithm SHA256).Hash.ToLower()
    if ($actual -ne $expected.ToLower()) {
        Remove-Item $file -Force -ErrorAction SilentlyContinue
        Fail "SHA-256 verification failed.`n    Expected: $expected`n    Actual:   $actual"
    }
}

# ─── PATH management ──────────────────────────────────────────────────────────

function Add-ToUserPath($dir) {
    $current = [System.Environment]::GetEnvironmentVariable("PATH", "User")
    $dirs = @($current -split ";" | Where-Object { $_ -ne "" })
    if ($dirs -contains $dir) {
        return $false  # already present
    }
    $newPath = ($dirs + $dir) -join ";"
    [System.Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    return $true
}

# ─── Main ─────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  Unity CLI Installer" -ForegroundColor White
Write-Host ""

$manifest = Get-Manifest

# Resolve version and binary info
$version = if ($Target -eq "latest") { $manifest.version } else { $Target }
$binaryInfo = $manifest.binaries.$platformKey

if (-not $binaryInfo) {
    $available = ($manifest.binaries | Get-Member -MemberType NoteProperty).Name -join ", "
    Fail "No binary available for platform '$platformKey'. Available: $available"
}

$filename = $binaryInfo.filename
$sha256   = $binaryInfo.sha256

Write-Info "Installing version $version for $platformKey..."

# Create install directory
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}

# Download to a temp path
$tmpFile = Join-Path $env:TEMP "unity-cli-install-$([System.Guid]::NewGuid().ToString('N')).exe"
$downloadUrl = "${CdnBase}${version}/$filename"

Get-Binary $downloadUrl $tmpFile

try {
    # Verify checksum. When a version is pinned via -Target, the manifest's checksum belongs
    # to the manifest's own version — not the pinned one — so skip verification.
    if ($Target -eq "latest") {
        Confirm-Sha256 $tmpFile $sha256
    } else {
        Write-Warn "Version pinned via -Target — skipping checksum verification."
    }

    # Move to install location
    $destFile = Join-Path $InstallDir "unity.exe"
    Move-Item -Path $tmpFile -Destination $destFile -Force

    # Add to user PATH
    $pathUpdated = Add-ToUserPath $InstallDir
}
catch {
    Remove-Item $tmpFile -Force -ErrorAction SilentlyContinue
    throw
}

# ─── Success ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "  " -NoNewline
Write-Host [char]0x2714 -ForegroundColor Green -NoNewline
Write-Host "  Unity CLI $version installed!" -ForegroundColor White
Write-Host ""
Write-Host "  Get started:" -ForegroundColor White
Write-Host "    unity --help           Show available commands"
Write-Host "    unity editors list     List installed Unity editors"
Write-Host "    unity install          Install a Unity editor"
Write-Host "    unity upgrade          Upgrade to the latest CLI version"
Write-Host ""

if ($pathUpdated) {
    Write-Host "  Note: Restart your terminal to use " -NoNewline
    Write-Host "unity" -ForegroundColor Cyan -NoNewline
    Write-Host "."
}
else {
    Write-Host "  " -NoNewline
    Write-Host "unity" -ForegroundColor Cyan -NoNewline
    Write-Host " is ready — open a new terminal and run " -NoNewline
    Write-Host "unity --help" -ForegroundColor Cyan -NoNewline
    Write-Host " to get started."
}

Write-Host ""
