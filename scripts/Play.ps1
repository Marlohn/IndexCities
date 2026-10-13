<#
.SYNOPSIS
    Abre o IndexCities na engine Godot (build mono/.NET).

.DESCRIPTION
    Antes de iniciar a partida, executa por padrão, nesta ordem:

        1) dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj
        2) <engine> --headless --path <repositório>\src\IndexCities.Godot --editor --import --quit
        3) <engine> --path <repositório>\src\IndexCities.Godot

    O passo 1 compila o host .NET e o passo 2 importa os recursos com a engine
    em modo headless, sem abrir janela. Se o build ou a importação falhar, a
    partida não é iniciada e o código de saída da etapa que falhou é propagado.
    Use -SkipBuild e -SkipImport para pular essas etapas.

    Ordem de descoberta:

        1) -GodotPath, quando informado (precisa apontar para um arquivo
           existente; caso contrário o script explica o problema e para);
        2) executáveis que combinam Godot*mono*.exe dentro de -GodotRoot.
           Os arquivos "*_console.exe" são ignorados porque são a variante de
           console, não a engine principal. Havendo mais de um, usa o de
           versão mais alta (v4.7.2 vence v4.7.1).

    O script não instala nada e não altera configuração global; ele usa apenas
    as ferramentas já instaladas (SDK do .NET e a engine Godot mono).

.PARAMETER GodotPath
    Caminho completo do executável da engine Godot mono. Quando informado, a
    descoberta automática é ignorada.

.PARAMETER GodotRoot
    Pasta onde a engine mono é procurada.
    Padrão: C:\Users\Marlohn\Tools\Godot

.PARAMETER GodotArguments
    Argumentos extras repassados à engine na partida, depois de --path. Eles não
    afetam o build nem a importação de recursos.

.PARAMETER SkipBuild
    Não compila o host .NET antes de iniciar a partida.

.PARAMETER SkipImport
    Não importa os recursos com a engine headless antes de iniciar a partida.

.PARAMETER DryRun
    Apenas descobre e valida a engine, mostra o plano completo (build,
    importação e partida) e não compila, não importa, não inicia a engine e não
    altera estado. Serve para conferir o script e o ambiente.

.EXAMPLE
    pwsh -File scripts/Play.ps1

.EXAMPLE
    pwsh -File scripts/Play.ps1 -GodotPath 'C:\Tools\Godot\Godot_v4.7.2-stable_mono_win64.exe'

.EXAMPLE
    pwsh -File scripts/Play.ps1 -SkipBuild -SkipImport

.EXAMPLE
    pwsh -File scripts/Play.ps1 -DryRun

.OUTPUTS
    0 em sucesso; o código de saída da engine quando o jogo termina com erro; o
    código de saída do build ou da importação quando essa etapa falha (a partida
    não é iniciada); 1 quando a engine, o projeto do jogo, o projeto .NET do
    host ou o SDK do .NET não são encontrados.

.NOTES
    Este script compila o host, importa os recursos e inicia a engine. Use
    -DryRun para conferir o plano sem executar nada, ou -SkipBuild/-SkipImport
    para pular etapas já feitas.

    Se o PowerShell recusar a execução do arquivo por política de execução, use
    um bypass válido apenas para aquele processo:

        powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Play.ps1

    O bypass vale só para esse processo e não altera configuração global.

    Ao chamar com -File, o Windows PowerShell entrega -GodotArguments como um
    único texto (por exemplo, "--headless,--verbose"). Para passar mais de um
    argumento extra, chame o script de dentro do PowerShell:

        & scripts/Play.ps1 -GodotArguments '--headless', '--verbose'
#>

[CmdletBinding()]
param(
    [string] $GodotPath,

    [string] $GodotRoot = 'C:\Users\Marlohn\Tools\Godot',

    [string[]] $GodotArguments = @(),

    [switch] $SkipBuild,

    [switch] $SkipImport,

    [switch] $DryRun
)

$ErrorActionPreference = 'Stop'

# Uma falha do dotnet ou da engine deve ser reportada pelo código de saída, não
# como exceção, para que o script possa parar antes de iniciar a partida.
if (Test-Path -LiteralPath 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

# --- Caminhos resolvidos a partir da pasta deste script -----------------------
$repoRoot        = Split-Path -Parent $PSScriptRoot
$godotProjectDir = Join-Path $repoRoot 'src\IndexCities.Godot'
$godotProject    = Join-Path $godotProjectDir 'IndexCities.Godot.csproj'
$projectFile     = Join-Path $godotProjectDir 'project.godot'

function Write-Step {
    param([string] $Message)

    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Invoke-External {
    param(
        [string]   $Title,
        [string]   $Executable,
        [string[]] $Arguments
    )

    Write-Step $Title
    Write-Host "    `"$Executable`" $($Arguments -join ' ')" -ForegroundColor DarkGray

    # Out-Host mantém a saída do processo fora do fluxo de saída da função, para
    # que o valor de retorno seja apenas o código de saída.
    & $Executable @Arguments | Out-Host
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "    OK: $Title" -ForegroundColor Green
    }
    else {
        Write-Host "    FALHOU (código $exitCode): $Title" -ForegroundColor Red
    }

    return $exitCode
}

function Get-MonoEngineCandidates {
    param([string] $SearchRoot)

    $candidates = @()

    if (-not (Test-Path -LiteralPath $SearchRoot -PathType Container)) {
        return $candidates
    }

    foreach ($file in Get-ChildItem -LiteralPath $SearchRoot -Filter '*.exe' -File -Recurse -ErrorAction SilentlyContinue) {
        if ($file.Name -notmatch '^Godot.*mono.*\.exe$') { continue }
        if ($file.Name -match '_console\.exe$') { continue }

        $version = [version]'0.0'
        $match = [regex]::Match($file.Name, 'v(\d+\.\d+(?:\.\d+)?)')
        if ($match.Success) {
            try { $version = [version]$match.Groups[1].Value } catch { }
        }

        $candidates += [pscustomobject]@{
            Path    = $file.FullName
            Name    = $file.Name
            Version = $version
        }
    }

    return $candidates
}

function Write-EngineNotFoundError {
    param([string] $SearchRoot)

    Write-Host 'ERRO: a engine Godot mono não foi encontrada.' -ForegroundColor Red
    Write-Host "      Pasta de busca: $SearchRoot" -ForegroundColor Red

    if (-not (Test-Path -LiteralPath $SearchRoot -PathType Container)) {
        Write-Host '      Essa pasta não existe.' -ForegroundColor Red
    }
    else {
        $found = @(Get-ChildItem -LiteralPath $SearchRoot -Filter '*.exe' -File -Recurse -ErrorAction SilentlyContinue)
        if ($found.Count -eq 0) {
            Write-Host '      Nenhum arquivo .exe foi encontrado nessa pasta.' -ForegroundColor Red
        }
        else {
            Write-Host '      Executáveis encontrados (nenhum serve como engine mono):' -ForegroundColor Red
            foreach ($file in $found) {
                Write-Host "        - $($file.Name)" -ForegroundColor DarkGray
            }
        }
    }

    Write-Host ''
    Write-Host '      Esperado: um executável da versão .NET/mono, por exemplo' -ForegroundColor Yellow
    Write-Host '        Godot_v4.7.2-stable_mono_win64.exe' -ForegroundColor Yellow
    Write-Host '      Coloque-o na pasta de busca ou informe o caminho explicitamente:' -ForegroundColor Yellow
    Write-Host '        pwsh -File scripts/Play.ps1 -GodotPath ''D:\Godot\Godot_v4.7.2-stable_mono_win64.exe''' -ForegroundColor Yellow
    Write-Host '      Também é possível trocar a pasta de busca com -GodotRoot.' -ForegroundColor Yellow
}

function Resolve-MonoEngine {
    param(
        [string] $ExplicitPath,
        [string] $SearchRoot
    )

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath -PathType Leaf)) {
            Write-Host 'ERRO: -GodotPath não aponta para um arquivo existente.' -ForegroundColor Red
            Write-Host "      Informado: $ExplicitPath" -ForegroundColor Red

            if (Test-Path -LiteralPath $ExplicitPath -PathType Container) {
                Write-Host '      Esse caminho é uma pasta; informe o executável (.exe) da engine.' -ForegroundColor Red
            }

            Write-Host "      Pasta padrão da engine mono: $SearchRoot" -ForegroundColor Yellow
            return $null
        }

        return (Get-Item -LiteralPath $ExplicitPath).FullName
    }

    $candidates = @(Get-MonoEngineCandidates -SearchRoot $SearchRoot)

    if ($candidates.Count -eq 0) {
        Write-EngineNotFoundError -SearchRoot $SearchRoot
        return $null
    }

    $best = $candidates | Sort-Object -Property Version -Descending | Select-Object -First 1

    if ($candidates.Count -gt 1) {
        Write-Host "Aviso: $($candidates.Count) engines mono encontradas; usando a de versão mais alta ($($best.Version))." -ForegroundColor Yellow
    }

    return $best.Path
}

# --- Projeto do jogo ---------------------------------------------------------
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    Write-Host 'ERRO: o projeto Godot do jogo não foi encontrado.' -ForegroundColor Red
    Write-Host "      Esperado: $projectFile" -ForegroundColor Red
    exit 1
}

# --- Engine ------------------------------------------------------------------
$engine = Resolve-MonoEngine -ExplicitPath $GodotPath -SearchRoot $GodotRoot
if (-not $engine) {
    exit 1
}

# --- Execução ----------------------------------------------------------------
$engineArguments = @('--path', $godotProjectDir)
if ($GodotArguments.Count -gt 0) {
    $engineArguments += $GodotArguments
}

# A importação roda em modo headless, sem abrir janela: --editor garante o
# contexto do editor e --quit encerra assim que os recursos forem importados.
$importArguments = @('--headless', '--path', $godotProjectDir, '--editor', '--import', '--quit')

$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue

Write-Host "Engine:  $engine" -ForegroundColor Cyan
Write-Host "Projeto: $godotProjectDir" -ForegroundColor Cyan

if ($DryRun) {
    Write-Step 'Plano (-DryRun: nada será executado)'

    if ($SkipBuild) {
        Write-Host '    1) build do host .NET: pulado (-SkipBuild)' -ForegroundColor DarkGray
    }
    elseif (-not $dotnetCommand) {
        Write-Host '    1) build do host .NET: INDISPONÍVEL (comando "dotnet" fora do PATH)' -ForegroundColor Red
    }
    else {
        Write-Host "    1) build do host .NET: dotnet build `"$godotProject`"" -ForegroundColor DarkGray
    }

    if ($SkipImport) {
        Write-Host '    2) importação de recursos: pulada (-SkipImport)' -ForegroundColor DarkGray
    }
    else {
        Write-Host "    2) importação de recursos: `"$engine`" $($importArguments -join ' ')" -ForegroundColor DarkGray
    }

    Write-Host "    3) partida: `"$engine`" $($engineArguments -join ' ')" -ForegroundColor DarkGray
    Write-Host '-DryRun: nada foi compilado, importado ou iniciado.' -ForegroundColor Yellow
    exit 0
}

# --- Build do host .NET ------------------------------------------------------
if (-not $SkipBuild) {
    if (-not (Test-Path -LiteralPath $godotProject -PathType Leaf)) {
        Write-Host 'ERRO: o projeto .NET do host não foi encontrado.' -ForegroundColor Red
        Write-Host "      Esperado: $godotProject" -ForegroundColor Red
        exit 1
    }

    if (-not $dotnetCommand) {
        Write-Host 'ERRO: o comando "dotnet" não foi encontrado no PATH.' -ForegroundColor Red
        Write-Host '      Instale o SDK do .NET (8.0 ou superior) e reabra o terminal.' -ForegroundColor Red
        exit 1
    }

    $buildExitCode = Invoke-External `
        -Title 'Build do host .NET' `
        -Executable $dotnetCommand.Source `
        -Arguments @('build', $godotProject)

    if ($buildExitCode -ne 0) {
        Write-Host "O build falhou (código $buildExitCode); a partida não será iniciada." -ForegroundColor Red
        exit $buildExitCode
    }
}

# --- Importação de recursos --------------------------------------------------
if (-not $SkipImport) {
    $importExitCode = Invoke-External `
        -Title 'Importação de recursos (Godot headless)' `
        -Executable $engine `
        -Arguments $importArguments

    if ($importExitCode -ne 0) {
        Write-Host "A importação falhou (código $importExitCode); a partida não será iniciada." -ForegroundColor Red
        exit $importExitCode
    }
}

# --- Partida -----------------------------------------------------------------
Write-Host 'Iniciando o IndexCities...' -ForegroundColor Cyan
& $engine @engineArguments
exit $LASTEXITCODE
