# Deploy script: mirror the whole project to the game's local Mods folder.
# Usage: powershell -NoProfile -File tools/deploy.ps1
# Rule: run after every successful build. Excludes .git / build intermediates.
# NOTE: keep this file ASCII-only (PowerShell 5.1 reads no-BOM UTF-8 as ANSI).

$src = $PSScriptRoot | Split-Path
$dst = "D:\Steam\steamapps\common\RimWorld\Mods\better_leader_and_preacher"

if (-not (Test-Path $dst)) { New-Item -ItemType Directory -Path $dst | Out-Null }

# /MIR mirror; /XD excluded dirs; /XF excluded files
robocopy $src $dst /MIR /XD .git bin obj .vs /XF *.pdb *.user | Out-Null
if ($LASTEXITCODE -ge 8) {
    Write-Host "DEPLOY FAILED (robocopy exit code $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}
Write-Host "Deployed to $dst (robocopy exit $LASTEXITCODE, <8 means success)" -ForegroundColor Green
