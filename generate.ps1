#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Generate benchmark submissions by running each AI CLI in HEADLESS mode on PROMPT.md.

  This automates the manual step: open `codex`/`claude`/`agy`, pick a model with /model,
  paste PROMPT.md, save the output. Each model runs non-interactively, writes a whole
  project into a fresh temp dir, and is filed as a submission run via `npm run add-run`.

.EXAMPLE
  ./generate.ps1                          # 1 run of every model in the matrix
  ./generate.ps1 -Runs 5                  # 5 runs each (proper k>=5 benchmarking)
  ./generate.ps1 -Only claude-opus-4-8-xhigh,gpt-5-5-xhigh
  ./generate.ps1 -Runs 5 -Eval            # generate, then grade + rebuild leaderboard
  ./generate.ps1 -DryRun                  # print what it would do, run nothing
#>
param(
    [int]$Runs = 1,
    [string[]]$Only,
    [switch]$Eval,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$root       = $PSScriptRoot
$promptFile = Join-Path $root 'PROMPT.md'
$evaluator  = Join-Path $root 'evaluator'
$workRoot   = Join-Path $env:TEMP 'benchmark-gen'

if (-not (Test-Path $promptFile)) { throw "PROMPT.md not found at $promptFile" }

# ---------------------------------------------------------------------------
#  MODEL MATRIX  ── edit this to add/remove models or fix exact model IDs.
#  label     : submission folder name (how it shows up on the leaderboard)
#  cli       : codex | claude | agy
#  modelArgs : the model-selection flags (this is your /model, as CLI flags)
#
#  ⚠ Confirm each model ID matches what the tool's /model picker shows. The
#    interactive display name and the --model/-m value sometimes differ; if a
#    run errors with "unknown model", fix the string here.
# ---------------------------------------------------------------------------
$matrix = @(
    @{ label = 'claude-opus-4-8-xhigh';   cli = 'claude'; modelArgs = @('--model', 'claude-opus-4-8',  '--effort', 'xhigh') }
    @{ label = 'claude-sonnet-4-6-xhigh'; cli = 'claude'; modelArgs = @('--model', 'claude-sonnet-4-6', '--effort', 'xhigh') }
    @{ label = 'claude-haiku-4-5';        cli = 'claude'; modelArgs = @('--model', 'claude-haiku-4-5') }
    @{ label = 'gpt-5-5-xhigh';           cli = 'codex';  modelArgs = @('-m', 'gpt-5.5-codex', '-c', 'model_reasoning_effort="high"') }
    @{ label = 'gemini-3-5-flash';        cli = 'agy';    modelArgs = @('--model', 'gemini-3.5-flash') }
    @{ label = 'gemini-3-5-pro';          cli = 'agy';    modelArgs = @('--model', 'gemini-3.5-pro') }
)

# agy (Antigravity CLI) blocks on an interactive "trust this folder?" prompt for any
# untrusted dir. Pre-add the workdir to ~/.gemini/trustedFolders.json so it doesn't hang.
function Ensure-AgyTrusted {
    param([string]$dir)
    $tf = Join-Path $env:USERPROFILE '.gemini\trustedFolders.json'
    if (-not (Test-Path $tf)) { return }          # agy not configured - nothing to do
    try { $obj = Get-Content $tf -Raw | ConvertFrom-Json } catch { return }
    if ($obj.PSObject.Properties.Name -contains $dir) { return }
    $obj | Add-Member -NotePropertyName $dir -NotePropertyValue 'TRUST_FOLDER' -Force
    $obj | ConvertTo-Json | Set-Content $tf -Encoding utf8
}

function Invoke-Agent {
    param($job, $workdir)
    $prompt = Get-Content -Raw $promptFile

    switch ($job.cli) {
        'codex' {
            # codex changes its own working root with -C; reads instructions from stdin via `-`
            $cliArgs = @('exec') + $job.modelArgs + @(
                '-C', $workdir,
                '--skip-git-repo-check',
                '--dangerously-bypass-approvals-and-sandbox',
                '-'
            )
            $prompt | & codex @cliArgs
        }
        'claude' {
            # claude works in the current directory; cd into the target first
            Push-Location $workdir
            try {
                $cliArgs = @('-p') + $job.modelArgs + @('--dangerously-skip-permissions')
                $prompt | & claude @cliArgs
            }
            finally { Pop-Location }
        }
        'agy' {
            # ⚠ KNOWN UPSTREAM BUG (antigravity-cli #76 / gemini-cli #27466, still open in
            #   v1.0.10): agy print mode HANGS / drops output unless stdout is a REAL TTY.
            #   => This leg only works when you run generate.ps1 from your own interactive
            #      terminal (agy inherits the console). It will NOT work headless / in CI /
            #      with stdout redirected, so do NOT capture its output here.
            Ensure-AgyTrusted $workdir
            Push-Location $workdir
            try {
                # prompt right after -p; inherit the console (no redirect, or it hangs)
                & agy -p $prompt @($job.modelArgs) --dangerously-skip-permissions --print-timeout 30m
            }
            finally { Pop-Location }
        }
        default { throw "Unknown cli '$($job.cli)' for label '$($job.label)'" }
    }
    if ($LASTEXITCODE -ne 0) { throw "$($job.cli) exited with code $LASTEXITCODE" }
}

$jobs = if ($Only) { $matrix | Where-Object { $_.label -in $Only } } else { $matrix }
if (-not $jobs) { throw "No matching models. Available: $(( $matrix.label) -join ', ')" }

New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
$generated = @()

foreach ($job in $jobs) {
    if (-not (Get-Command $job.cli -ErrorAction SilentlyContinue)) {
        Write-Warning "CLI '$($job.cli)' not on PATH - skipping $($job.label)"
        continue
    }

    for ($i = 1; $i -le $Runs; $i++) {
        $stamp   = Get-Date -Format 'yyyyMMdd-HHmmss'
        $workdir = Join-Path $workRoot "$($job.label)__$stamp-$i"
        New-Item -ItemType Directory -Force -Path $workdir | Out-Null

        Write-Host ""
        Write-Host "==> $($job.label)  run $i/$Runs  ($($job.cli))" -ForegroundColor Cyan
        Write-Host "    workdir: $workdir" -ForegroundColor DarkGray
        if ($DryRun) {
            Write-Host "    (dry run - skipping agent + add-run)" -ForegroundColor DarkGray
            continue
        }

        try {
            Invoke-Agent $job $workdir

            # file it as a submission run (add-run picks the next runN automatically)
            Push-Location $evaluator
            try { npm run --silent add-run -- $job.label $workdir }
            finally { Pop-Location }

            $generated += $job.label
            Write-Host "    OK -> filed as a run of '$($job.label)'" -ForegroundColor Green
        }
        catch {
            Write-Warning "    FAILED ($($job.label) run $i): $($_.Exception.Message)"
        }
    }
}

Write-Host ""
Write-Host "Generated $($generated.Count) run(s)." -ForegroundColor Cyan

if ($Eval -and -not $DryRun -and $generated.Count -gt 0) {
    Push-Location $evaluator
    try {
        npm run --silent eval
        npm run --silent eval -- --leaderboard
    }
    finally { Pop-Location }
}
