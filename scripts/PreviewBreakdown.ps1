<# Captura carro, caminhao e frota com capo aberto, triangulo, pisca e fumaca; nao acessa cidades/saves. #>
[CmdletBinding()]
param(
    [string] $GodotPath = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
$assetRepo = Split-Path -Parent $PSScriptRoot
$assetEvidence = Join-Path $assetRepo 'artifacts\verification\quebras-assets'
$assetArgs = @('--path', (Join-Path $assetRepo 'src\IndexCities.Godot'), '--rendering-method', 'gl_compatibility', '--resolution', '1280x800', '--script', 'res://Assets/Breakdown/Source/preview_breakdown.gd', '--', ('--out='+$assetEvidence))
& $GodotPath @assetArgs
exit $LASTEXITCODE
