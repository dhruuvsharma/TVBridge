<#
.SYNOPSIS
    TVBridge release build script — publishes the app, bundles the Python sidecar,
    downloads cloudflared, and prepares the dist/ directory for Inno Setup.

.DESCRIPTION
    Output structure:
        dist/
            app/            — .NET 8 self-contained publish output
            sidecar/        — Python sidecar source + requirements
            cloudflared/    — cloudflared.exe (downloaded if missing)
            python/         — Python embeddable package (downloaded if missing)

.PARAMETER Configuration
    Build configuration (default: Release)

.PARAMETER Runtime
    Target runtime identifier (default: win-x64)

.PARAMETER SkipDownloads
    Skip downloading cloudflared and Python embeddable

.EXAMPLE
    .\scripts\release-build.ps1
    .\scripts\release-build.ps1 -SkipDownloads
#>

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SkipDownloads
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $RepoRoot "dist"
$PythonVersion = "3.11.9"
$CloudflaredVersion = "2024.10.0"

Write-Host "=== TVBridge Release Build ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Runtime: $Runtime"
Write-Host "Dist: $DistDir"
Write-Host ""

# Clean dist
if (Test-Path $DistDir) {
    Write-Host "Cleaning dist/..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $DistDir
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Step 1: dotnet publish
Write-Host "`n[1/5] Publishing .NET app..." -ForegroundColor Green
$AppDist = Join-Path $DistDir "app"
dotnet publish "$RepoRoot\src\TVBridge.App\TVBridge.App.csproj" `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained true `
    --output $AppDist `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=false
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

# Step 2: Copy sidecar
Write-Host "`n[2/5] Copying Python sidecar..." -ForegroundColor Green
$SidecarDist = Join-Path $DistDir "sidecar\mt5_bridge"
New-Item -ItemType Directory -Path $SidecarDist -Force | Out-Null
Copy-Item "$RepoRoot\sidecar\mt5_bridge\*.py" $SidecarDist -Force
Copy-Item "$RepoRoot\sidecar\mt5_bridge\requirements.txt" $SidecarDist -Force
Copy-Item "$RepoRoot\sidecar\mt5_bridge\pyproject.toml" $SidecarDist -Force

# Step 3: Download Python embeddable (if needed)
$PythonDist = Join-Path $DistDir "python"
if (-not $SkipDownloads) {
    Write-Host "`n[3/5] Downloading Python embeddable $PythonVersion..." -ForegroundColor Green
    $PythonZip = Join-Path $env:TEMP "python-$PythonVersion-embed-amd64.zip"
    if (-not (Test-Path $PythonZip)) {
        $PythonUrl = "https://www.python.org/ftp/python/$PythonVersion/python-$PythonVersion-embed-amd64.zip"
        Write-Host "  Downloading from $PythonUrl"
        Invoke-WebRequest -Uri $PythonUrl -OutFile $PythonZip -UseBasicParsing
    }
    New-Item -ItemType Directory -Path $PythonDist -Force | Out-Null
    Expand-Archive -Path $PythonZip -DestinationPath $PythonDist -Force

    # Enable pip in embeddable Python: uncomment import site in python311._pth
    $PthFile = Get-ChildItem $PythonDist -Filter "python*._pth" | Select-Object -First 1
    if ($PthFile) {
        $content = Get-Content $PthFile.FullName
        $content = $content -replace "^#import site", "import site"
        Set-Content $PthFile.FullName $content
        Write-Host "  Enabled import site in $($PthFile.Name)"
    }
} else {
    Write-Host "`n[3/5] Skipping Python download" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PythonDist -Force | Out-Null
}

# Step 4: Download cloudflared (if needed)
$CloudflaredDist = Join-Path $DistDir "cloudflared"
if (-not $SkipDownloads) {
    Write-Host "`n[4/5] Downloading cloudflared..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $CloudflaredDist -Force | Out-Null
    $CloudflaredExe = Join-Path $CloudflaredDist "cloudflared.exe"
    if (-not (Test-Path $CloudflaredExe)) {
        $CloudflaredUrl = "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
        Write-Host "  Downloading from $CloudflaredUrl"
        Invoke-WebRequest -Uri $CloudflaredUrl -OutFile $CloudflaredExe -UseBasicParsing
    }
} else {
    Write-Host "`n[4/5] Skipping cloudflared download" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $CloudflaredDist -Force | Out-Null
}

# Step 5: Run tests
Write-Host "`n[5/5] Running tests..." -ForegroundColor Green
dotnet test "$RepoRoot" --configuration $Configuration --no-build --verbosity minimal
if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

# Summary
Write-Host "`n=== Build Complete ===" -ForegroundColor Cyan
Write-Host "Dist directory: $DistDir"
$AppSize = (Get-ChildItem $AppDist -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB
Write-Host "  app/        $([math]::Round($AppSize, 1)) MB"
if (Test-Path (Join-Path $CloudflaredDist "cloudflared.exe")) {
    $CfSize = (Get-Item (Join-Path $CloudflaredDist "cloudflared.exe")).Length / 1MB
    Write-Host "  cloudflared/ $([math]::Round($CfSize, 1)) MB"
}
Write-Host ""
Write-Host "Next: Run Inno Setup on installer/tvbridge.iss" -ForegroundColor Yellow
