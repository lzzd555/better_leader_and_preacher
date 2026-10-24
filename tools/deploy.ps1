# Deploy script: copy RUNTIME-ONLY mod content (About/LoadFolders/1.7) to the
# single fixed game mod folder. That folder is both the playtest copy and the
# workshop upload copy - keep it clean (no Source/docs/tools).
# Usage: powershell -NoProfile -File tools/deploy.ps1
# NOTE: keep this file ASCII-only (PowerShell 5.1 reads no-BOM UTF-8 as ANSI).

$src = $PSScriptRoot | Split-Path
$dst = "D:\Steam\steamapps\common\RimWorld\Mods\Better Leader and Moral Guide"

if (Get-Process -Name "RimWorld" -ErrorAction SilentlyContinue) {
    Write-Host "RimWorld is RUNNING - the game locks mod DLLs. Close the game first, then redeploy." -ForegroundColor Yellow
    exit 2
}

if (-not (Test-Path $dst)) { New-Item -ItemType Directory -Path $dst | Out-Null }

robocopy "$src\About" "$dst\About" /MIR /R:1 /W:1 | Out-Null
robocopy "$src\1.7" "$dst\1.7" /MIR /R:1 /W:1 | Out-Null
Copy-Item "$src\LoadFolders.xml" "$dst\LoadFolders.xml" -Force

if ($LASTEXITCODE -ge 8) {
    Write-Host "DEPLOY FAILED (robocopy exit code $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}
Write-Host "Deployed runtime files to $dst (robocopy exit $LASTEXITCODE, <8 means success)" -ForegroundColor Green
