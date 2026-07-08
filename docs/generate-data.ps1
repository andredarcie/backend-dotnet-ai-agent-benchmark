#!/usr/bin/env pwsh
<#
.SYNOPSIS
  Build the leaderboard data the docs site reads.

  Scans evaluator-dotnet/results/*.dotnet.json (one file per graded run), groups
  runs per model, computes the per-model median / spread the same way the .NET
  LeaderboardReporter does, and writes two artifacts under docs/data/:

    benchmark.json  - the raw, machine-readable data
    data.js         - the same payload as `window.__BENCHMARK__ = {...};`
                      (a plain <script> the site loads; works over file:// too,
                       no fetch/CORS, no server needed)

  Re-run this whenever you add or re-grade a run and the site updates itself.

.EXAMPLE
  ./docs/generate-data.ps1
  ./docs/generate-data.ps1 -ResultsDir ../some/other/results
#>
param(
    [string]$ResultsDir,
    [string]$OutDir
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $ResultsDir) { $ResultsDir = Join-Path $root '..\evaluator-dotnet\results' }
if (-not $OutDir)     { $OutDir     = Join-Path $root 'data' }

# Models with fewer than this many deep runs are flagged provisional
# (mirrors Leaderboard.ProvisionalThreshold in the .NET evaluator).
$ProvisionalThreshold = 5

if (-not (Test-Path $ResultsDir)) { throw "Results dir not found: $ResultsDir" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Get-Median {
    param([double[]]$Values)
    $s = @($Values | Sort-Object)
    $n = $s.Count
    if ($n -eq 0) { return 0.0 }
    if ($n % 2 -eq 1) { return [double]$s[[int](($n - 1) / 2)] }
    return ([double]$s[$n / 2 - 1] + [double]$s[$n / 2]) / 2.0
}

function Get-StdDev {
    param([double[]]$Values)
    $n = $Values.Count
    if ($n -lt 2) { return 0.0 }
    $mean = ($Values | Measure-Object -Average).Average
    $sum = 0.0
    foreach ($v in $Values) { $sum += [math]::Pow($v - $mean, 2) }
    return [math]::Sqrt($sum / $n)   # population sigma, matching the reporter
}

# ---- read every run report --------------------------------------------------
$files = Get-ChildItem -Path $ResultsDir -Filter '*.dotnet.json' -File |
    Where-Object { $_.Name -ne 'leaderboard.dotnet.json' }

$runs = @()
foreach ($f in $files) {
    try { $r = Get-Content $f.FullName -Raw | ConvertFrom-Json }
    catch { Write-Warning "skip (bad JSON): $($f.Name)"; continue }

    $target = [string]$r.Target
    $parts  = $target -split '[\\/]', 2
    $model  = $parts[0]
    $run    = if ($parts.Count -gt 1) { $parts[1] } else { 'run1' }

    $categories = @()
    foreach ($c in $r.Categories) {
        $metrics = @()
        foreach ($m in $c.Metrics) {
            $metrics += [ordered]@{
                name     = $m.Name
                observed = $m.Observed
                target   = $m.Target
                status   = $m.Status      # Pass | Partial | Fail | Indeterminate
                weight   = $m.Weight
                note     = $m.Note
            }
        }
        $categories += [ordered]@{
            number              = $c.Number
            name                = $c.Name
            weight              = $c.Weight
            automation          = $c.Automation   # FullAuto | SemiOracle | ProxyReview (measurement method; all 100% automated)
            badge               = $c.Badge
            score               = $c.Score
            measuredCount       = $c.MeasuredCount
            indeterminateCount  = $c.IndeterminateCount
            notes               = @($c.Notes)
            missingTools        = @($c.MissingTools)
            metrics             = $metrics
        }
    }

    $runs += [ordered]@{
        target         = $target
        model          = $model
        run            = $run
        evaluatedAtUtc = $r.EvaluatedAtUtc
        deep           = [bool]$r.Deep
        weightedScore  = $r.WeightedScore
        coverage       = $r.Coverage
        builds         = [bool]$r.Builds
        boots          = [bool]$r.Boots
        scoreCapReason = $r.ScoreCapReason
        environment    = @($r.Environment)
        categories     = $categories
    }
}

# ---- aggregate per model (deep runs only, like the reporter) ----------------
# NB: the run objects are OrderedDictionaries, so Group/Sort must read keys via
# a script block ({ $_.model }), not a bare -Property name (which returns null).
$leaderboard = @()
$deepRuns = @($runs | Where-Object { $_.deep })
$byModel  = $deepRuns | Group-Object -Property { $_.model } | Sort-Object Name

foreach ($g in $byModel) {
    $scores = @($g.Group | ForEach-Object { [double]$_.weightedScore })
    $median = Get-Median $scores
    $mean   = ($scores | Measure-Object -Average).Average
    $min    = ($scores | Measure-Object -Minimum).Minimum
    $max    = ($scores | Measure-Object -Maximum).Maximum
    $sigma  = Get-StdDev $scores

    $leaderboard += [ordered]@{
        model       = $g.Name
        runs        = $g.Count
        median      = [math]::Round($median, 2)
        mean        = [math]::Round($mean, 2)
        stdDev      = [math]::Round($sigma, 2)
        min         = [math]::Round($min, 2)
        max         = [math]::Round($max, 2)
        provisional = ($g.Count -lt $ProvisionalThreshold)
        allBuild    = (@($g.Group | Where-Object { -not $_.builds }).Count -eq 0)
        allBoot     = (@($g.Group | Where-Object { -not $_.boots }).Count -eq 0)
    }
}

# rank by median desc, then more runs first, then model name
$leaderboard = @($leaderboard | Sort-Object `
    @{ Expression = { $_.median }; Descending = $true }, `
    @{ Expression = { $_.runs };   Descending = $true }, `
    @{ Expression = { $_.model };  Descending = $false })

$rank = 1
foreach ($row in $leaderboard) { $row['rank'] = $rank; $rank++ }

$lightRunCount = @($runs | Where-Object { -not $_.deep }).Count

$payload = [ordered]@{
    generatedAtUtc       = (Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ssZ')
    provisionalThreshold = $ProvisionalThreshold
    modelCount           = $leaderboard.Count
    runCount             = $runs.Count
    deepRunCount         = $deepRuns.Count
    lightRunCount        = $lightRunCount
    leaderboard          = $leaderboard
    runs                 = @($runs | Sort-Object -Property { $_.target })
}

$json = $payload | ConvertTo-Json -Depth 12

$jsonPath = Join-Path $OutDir 'benchmark.json'
$jsPath   = Join-Path $OutDir 'data.js'

# UTF-8 without BOM so the browser is happy
$enc = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($jsonPath, $json, $enc)
[System.IO.File]::WriteAllText($jsPath, "// AUTO-GENERATED by docs/generate-data.ps1 - do not edit by hand.`nwindow.__BENCHMARK__ = $json;`n", $enc)

Write-Host "Wrote $($leaderboard.Count) model(s), $($runs.Count) run(s):" -ForegroundColor Cyan
Write-Host "  $jsonPath"
Write-Host "  $jsPath"
