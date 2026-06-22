#!/usr/bin/env pwsh
# Convenience wrapper: runs the evaluator from the repo root.
#
#   ./run.ps1            # evaluate every submission in submissions/
#   ./run.ps1 reference  # evaluate a single submission by folder name
#   ./run.ps1 reference --no-stress

param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Args
)

$ErrorActionPreference = "Stop"
$evaluator = Join-Path $PSScriptRoot "evaluator"

if (-not (Test-Path (Join-Path $evaluator "node_modules"))) {
    Write-Host "Installing evaluator dependencies..." -ForegroundColor Cyan
    Push-Location $evaluator
    npm install
    Pop-Location
}

Push-Location $evaluator
try {
    npm run --silent eval -- @Args
}
finally {
    Pop-Location
}
