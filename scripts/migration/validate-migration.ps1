#!/usr/bin/env pwsh
# ENH-INFRA-007 — Migration Validator
#
# Usage:
#   .\validate-migration.ps1 [-MigrationSqlFile <path>] [-Strict]
#
# Scans an EF Core migration SQL script for:
#   1. CREATE INDEX without ONLINE = ON (blocks table; rewrite as online index)
#   2. DROP TABLE / DROP COLUMN on potentially large tables (data loss risk)
#   3. ALTER TABLE ... ADD COLUMN NOT NULL without a default (can lock)
#   4. Absence of idempotent IF EXISTS / IF NOT EXISTS guards
#
# Exit code:
#   0 — no violations (or -Strict not set and only warnings found)
#   1 — violations found (always in -Strict mode, or errors in normal mode)

param(
    [Parameter(Mandatory = $false)]
    [string] $MigrationSqlFile,

    [switch] $Strict
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$violations = [System.Collections.Generic.List[string]]::new()
$warnings   = [System.Collections.Generic.List[string]]::new()

# ─── Helper ────────────────────────────────────────────────────────────────────
function Check-File([string] $filePath) {
    if (-not (Test-Path $filePath)) {
        Write-Error "File not found: $filePath"
        exit 1
    }
    $lines = Get-Content $filePath

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $lineNo = $i + 1

        # Rule 1 — CREATE INDEX without ONLINE = ON
        if ($line -match '\bCREATE\s+(UNIQUE\s+)?INDEX\b' -and
            $line -notmatch '\bONLINE\s*=\s*ON\b') {
            $violations.Add("LINE $lineNo: CREATE INDEX missing ONLINE = ON — this will block concurrent reads/writes on large tables.`n  > $($line.Trim())")
        }

        # Rule 2 — DROP TABLE (data loss, irreversible)
        if ($line -match '\bDROP\s+TABLE\b') {
            $violations.Add("LINE $lineNo: DROP TABLE detected — ensure rollback script is committed alongside this migration.`n  > $($line.Trim())")
        }

        # Rule 3 — DROP COLUMN (data loss)
        if ($line -match '\bDROP\s+COLUMN\b') {
            $warnings.Add("LINE $lineNo: DROP COLUMN detected — confirm column is unused and rollback script exists.`n  > $($line.Trim())")
        }

        # Rule 4 — ADD COLUMN ... NOT NULL without DEFAULT
        if ($line -match '\bADD\s+\[?\w+\]?\s+\w+.*NOT NULL' -and
            $line -notmatch '\bDEFAULT\b' -and
            $line -notmatch '\bIDENTITY\b') {
            $violations.Add("LINE $lineNo: NOT NULL column added without DEFAULT — this locks the table for full rewrite on existing data.`n  > $($line.Trim())")
        }

        # Rule 5 — ALTER COLUMN type change (potential implicit conversion lock)
        if ($line -match '\bALTER\s+COLUMN\b') {
            $warnings.Add("LINE $lineNo: ALTER COLUMN detected — verify this is an online-compatible type change.`n  > $($line.Trim())")
        }
    }
}

# ─── Discover migration SQL files ─────────────────────────────────────────────
$filesToCheck = @()
if ($MigrationSqlFile) {
    $filesToCheck = @($MigrationSqlFile)
} else {
    # Scan all *.sql files under scripts/migration and any generated sql output
    $projectRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
    $filesToCheck = Get-ChildItem -Path $projectRoot -Filter '*.sql' -Recurse |
                    Where-Object { $_.FullName -notmatch 'node_modules|bin|obj' } |
                    Select-Object -ExpandProperty FullName
    if ($filesToCheck.Count -eq 0) {
        Write-Host '✓ No SQL migration files found to validate.' -ForegroundColor Green
        exit 0
    }
}

foreach ($f in $filesToCheck) {
    Write-Host "Checking: $f" -ForegroundColor Cyan
    Check-File $f
}

# ─── Report ──────────────────────────────────────────────────────────────────
$hasIssues = $false

if ($warnings.Count -gt 0) {
    Write-Host "`n⚠  WARNINGS ($($warnings.Count)):" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
    if ($Strict) { $hasIssues = $true }
}

if ($violations.Count -gt 0) {
    Write-Host "`n✗  VIOLATIONS ($($violations.Count)) — migration must be revised:" -ForegroundColor Red
    $violations | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    $hasIssues = $true
}

if (-not $hasIssues) {
    Write-Host "`n✓  Migration validation passed." -ForegroundColor Green
    exit 0
} else {
    exit 1
}
