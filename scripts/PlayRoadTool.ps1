# First human evaluation of the real road tool, with a separate city directory.
param([switch] $EmptyCity, [switch] $SkipBuild, [switch] $SkipImport)
$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
$saveFolder=Join-Path $repository 'artifacts/verification/road-tool-manual/cities'
$logFolder=Join-Path $repository 'artifacts/verification/road-tool-manual'
New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
$arguments=@('--log-file',(Join-Path $logFolder 'godot.log'),'--',('--review-save-dir='+$saveFolder))
if(!$EmptyCity){$arguments+='--road-tool-playtest'}
& (Join-Path $PSScriptRoot 'Play.ps1') -SkipBuild:$SkipBuild -SkipImport:$SkipImport -GodotArguments $arguments
