#!/usr/bin/env pwsh
# ENH-INFRA-007 — Rollback Script Generator
#
# Generates a SQL script to roll back from a target migration to the previous one.
# The script is written to docs/rollback-scripts/<migration-name>-rollback.sql.
#
# Usage:
#   .\generate-rollback.ps1 -TargetMigration <name>
#
# Example:
#   .\generate-rollback.ps1 -TargetMigration Phase14_Analytics_AddZeroResultCount
#
# Requires: dotnet-ef tool installed globally or as a local tool.

param(
    [Parameter(Mandatory)]
    [string] $TargetMigration
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot       = Resolve-Path (Join-Path $PSScriptRoot '../..')
$infraProject      = Join-Path $projectRoot 'backend/src/Shared/StyleNest.Infrastructure'
$rollbackDir       = Join-Path $projectRoot 'docs/rollback-scripts'
$rollbackSqlFile   = Join-Path $rollbackDir "$TargetMigration-rollback.sql"

# ─── Ensure output directory exists ──────────────────────────────────────────
if (-not (Test-Path $rollbackDir)) {
    New-Item -ItemType Directory -Path $rollbackDir | Out-Null
}

Write-Host "Generating rollback script for migration: $TargetMigration" -ForegroundColor Cyan

# ─── Get ordered list of migrations ──────────────────────────────────────────
$migrationsRaw = dotnet ef migrations list --project $infraProject --no-build 2>&1 | Where-Object { $_ -notmatch '^Build' }
$migrations    = $migrationsRaw | Where-Object { $_.Trim() -ne '' } | ForEach-Object { $_.Trim() -replace ' \(Pending\)', '' }

$targetIndex = $migrations.IndexOf($TargetMigration)
if ($targetIndex -lt 0) {
    Write-Error "Migration '$TargetMigration' not found. Available migrations:`n$($migrations -join "`n")"
    exit 1
}

$previousMigration = if ($targetIndex -gt 0) { $migrations[$targetIndex - 1] } else { '0' }

Write-Host "  Rolling back: $TargetMigration  →  $previousMigration" -ForegroundColor Yellow

# ─── Generate script (EF Core idempotent rollback) ──────────────────────────
# EF generates a script from $previousMigration back to the state before $TargetMigration
# by specifying --from=current --to=previous
$scriptArgs = @(
    'ef', 'migrations', 'script',
    '--project', $infraProject,
    '--output', $rollbackSqlFile,
    '--idempotent',
    '--no-build',
    $TargetMigration,  # from (current)
    $previousMigration # to (roll back to this)
)

Write-Host "Running: dotnet $($scriptArgs -join ' ')" -ForegroundColor DarkGray
& dotnet @scriptArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet ef migrations script failed (exit $LASTEXITCODE)"
    exit 1
}

# ─── Prepend rollback header ──────────────────────────────────────────────────
$header = @"
-- ╔══════════════════════════════════════════════════════════════════════════════╗
-- ║  ENH-INFRA-007 — ROLLBACK SCRIPT                                           ║
-- ║  Migration  : $TargetMigration
-- ║  Roll back to: $previousMigration
-- ║  Generated  : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') UTC                              ║
-- ║                                                                              ║
-- ║  ⚠  REVIEW BEFORE RUNNING — verify each statement against production state  ║
-- ║  ⚠  Run in a transaction: BEGIN TRAN / COMMIT or ROLLBACK                  ║
-- ╚══════════════════════════════════════════════════════════════════════════════╝

BEGIN TRANSACTION;
-- [PASTE ROLLBACK STEPS ABOVE AND UNCOMMENT COMMIT BELOW AFTER REVIEW]
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;

"@

$existingContent = Get-Content $rollbackSqlFile -Raw
Set-Content $rollbackSqlFile ($header + $existingContent)

Write-Host "`n✓  Rollback script written to: $rollbackSqlFile" -ForegroundColor Green
Write-Host "   Review the script carefully before running in production." -ForegroundColor Yellow
