$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$baseline = Join-Path $PSScriptRoot 'main-13b8e98-source'
$before = Join-Path $baseline 'src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll'
$after = Join-Path $root 'src/IndexCities.Headless/bin/Release/net8.0/IndexCities.Headless.dll'
$metadata = Join-Path $PSScriptRoot 'resume-bench-metadata.txt'
@(
    "UTC=$([DateTime]::UtcNow.ToString('o'))"
    'before=origin/main 13b8e98 (git archive; simulation and tests unchanged)'
    'after=de62d24 (includes main 13b8e98 and review corrections)'
    'before harness: only Headless/Program.cs and Fixtures.cs copied from after for identical metrics and seed43 valid intake access'
    'sequential order: cycle 42/7/43, grid 42/7/43; before then after per pair; no concurrent builds in this run'
) | Set-Content -LiteralPath $metadata -Encoding UTF8
foreach ($sourceRoot in @($baseline,$root)) {
    foreach ($relative in @('src/IndexCities.Headless/Program.cs','src/IndexCities.Headless/Fixtures.cs','src/IndexCities.Simulation/bin/Release/net8.0/IndexCities.Simulation.dll')) {
        $hash = Get-FileHash -LiteralPath (Join-Path $sourceRoot $relative) -Algorithm SHA256
        "$($hash.Hash) $($hash.Path)" | Add-Content -LiteralPath $metadata -Encoding UTF8
    }
}
foreach ($scenario in @('cycle','grid')) {
    foreach ($seed in @(42,7,43)) {
        $benchArgs = if ($scenario -eq 'cycle') { @('scale','cycle','--days=7','--warmup=2','--cap=3600',"--seed=$seed") } else { @('stuck','grid','--rows=3','--length=400','--days=11','--warmup=2','--cap=3600',"--seed=$seed") }
        foreach ($version in @('before','after')) {
            $binary = if ($version -eq 'before') { $before } else { $after }
            $log = Join-Path $PSScriptRoot "resume-$version-$scenario-$seed.log"
            Write-Output "START $version $scenario $seed"
            & dotnet $binary @benchArgs *> $log
            $exitCode = $LASTEXITCODE
            if ($exitCode -ne 0) { throw "Bench failed: $log exit=$exitCode" }
            $lines = Get-Content -LiteralPath $log
            $expected = if ($scenario -eq 'cycle') { 7 } else { 11 }
            if (!($lines -match '^INVARIANTS OK$') -or !($lines -match "^SUMMARY days=$expected ")) { throw "Bench incomplete: $log" }
            $lines | Where-Object { $_ -match '^DAY (7 |11 )|^SUMMARY |^STUCK SUMMARY |^SAVE |^INVARIANTS OK' } | Write-Output
            Write-Output "DONE $version $scenario $seed"
        }
    }
}
