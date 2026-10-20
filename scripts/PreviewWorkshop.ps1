<# Abre somente a preview do asset futuro; nao acessa cidades/saves. #>
[CmdletBinding()]
param(
    [string] $GodotPath = 'C:\Users\Marlohn\Tools\Godot\Godot_v4.7.2-stable_mono_win64_console.exe',
    [ValidateSet('game','studio')] [string] $Lighting = 'game',
    [switch] $CaptureOnly
)
$ErrorActionPreference = 'Stop'
$assetRepo = Split-Path -Parent $PSScriptRoot
$assetEvidence = Join-Path $assetRepo 'artifacts\verification\mechanic-shop-codex'
$assetRenderer = if ($Lighting -eq 'game') { 'gl_compatibility' } else { 'forward_plus' }
$assetArgs = @('--path', (Join-Path $assetRepo 'src\IndexCities.Godot'), '--rendering-method', $assetRenderer, '--resolution', '1280x800', '--script', 'res://Assets/Candidates/MechanicShopCodex/Source/preview.gd', '--', ('--out='+$assetEvidence), ('--lighting='+$Lighting))
if (-not $CaptureOnly) { $assetArgs += @('--views=hero','--inspect') }
& $GodotPath @assetArgs
exit $LASTEXITCODE
