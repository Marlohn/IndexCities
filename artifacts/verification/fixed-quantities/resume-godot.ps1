$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$project = Join-Path $root 'src/IndexCities.Godot'
$engine = 'C:/Users/Marlohn/Tools/Godot/Godot_v4.7.2-stable_mono_win64_console.exe'
$saveDir = Join-Path $PSScriptRoot 'resume-godot-saves'
& dotnet build (Join-Path $project 'IndexCities.Godot.csproj') -t:Rebuild --nologo *> (Join-Path $PSScriptRoot 'resume-godot-build.log')
if ($LASTEXITCODE -ne 0) { throw 'Godot rebuild failed' }
& $engine --headless --path $project --editor --import --quit *> (Join-Path $PSScriptRoot 'resume-godot-import.log')
if ($LASTEXITCODE -ne 0) { throw 'Godot import failed' }
$assembly = Join-Path $project '.godot/mono/temp/bin/Debug/IndexCities.Simulation.dll'
$hash = Get-FileHash -LiteralPath $assembly -Algorithm SHA256
"$($hash.Hash) $($hash.Path)" | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'resume-godot-assembly.txt') -Encoding UTF8
foreach ($scenario in @('qa-list','smoke','persistence-smoke')) {
    $log = Join-Path $PSScriptRoot "resume-godot-$scenario.log"
    & $engine --headless --path $project -- "--$scenario" "--review-save-dir=$saveDir" *> $log
    if ($LASTEXITCODE -ne 0) { throw "Godot $scenario failed" }
    $lines = Get-Content -LiteralPath $log
    $lines | Write-Output
    if ($scenario -eq 'smoke' -and !($lines -match 'INDEXCITIES_SMOKE_OK buildings=0 roads=17')) { throw 'Smoke marker missing' }
    if ($scenario -eq 'persistence-smoke') {
        $marker = $lines | Where-Object { $_ -match 'INDEXCITIES_PERSISTENCE_OK' }
        if (!$marker -or !($marker -like "*directory=$saveDir*")) { throw 'Persistence marker/save directory missing' }
    }
}
