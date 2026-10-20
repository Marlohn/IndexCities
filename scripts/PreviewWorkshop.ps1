<# Captura a oficina com veiculos reais nas ancoras; nao acessa cidades/saves. #>
[CmdletBinding()]
param(
    [string] $GodotPath = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe',
    [ValidateSet('game','studio')] [string] $Lighting = 'game',
    [switch] $CaptureOnly
)
$ErrorActionPreference = 'Stop'
$assetRepo = Split-Path -Parent $PSScriptRoot
$assetEvidence = Join-Path $assetRepo 'artifacts\verification\quebras-assets'
$assetRenderer = if ($Lighting -eq 'game') { 'gl_compatibility' } else { 'forward_plus' }
$assetArgs = @('--path', (Join-Path $assetRepo 'src\IndexCities.Godot'), '--rendering-method', $assetRenderer, '--resolution', '1280x800', '--script', 'res://Assets/Workshop/Source/preview_workshop.gd', '--', ('--out='+$assetEvidence))

& $GodotPath @assetArgs
exit $LASTEXITCODE
