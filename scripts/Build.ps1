<#
.SYNOPSIS
    Compila o projeto Godot e executa os testes da simulação.

.DESCRIPTION
    Executa, nesta ordem:

        1) dotnet build src/IndexCities.Godot/IndexCities.Godot.csproj
        2) dotnet test  tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj

    Com -LocalAssertions, a etapa 2 passa a ser um caminho explícito e
    alternativo: compila a assembly de testes e executa as mesmas assertions
    pelo entry point local da própria assembly:

        dotnet build tests/IndexCities.Simulation.Tests/IndexCities.Simulation.Tests.csproj
        dotnet tests/IndexCities.Simulation.Tests/bin/<Configuration>/net8.0/IndexCities.Simulation.Tests.dll

    Esse caminho existe porque o runner VSTest falha neste ambiente ao abrir o
    processo pai (Process.OpenProcess com acesso negado). Ele nunca é escolhido
    automaticamente: sem a opção, o padrão continua sendo dotnet test. Uma falha
    continua sendo reportada pelo código de saída; se o build do projeto de
    testes falhar, as assertions locais não são executadas sobre uma assembly
    antiga e a etapa é reportada como não executada.

    As etapas são executadas em sequência mesmo que uma falhe, para que o resumo
    final mostre o estado real de build e de testes. O código de saída é 1 se
    qualquer etapa executada falhar.

    O script não abre a engine, não instala dependências e não altera
    configuração global. Todos os caminhos são resolvidos a partir de
    $PSScriptRoot, então ele funciona de qualquer diretório de trabalho.

.PARAMETER Configuration
    Configuração do MSBuild. Padrão: Debug.

.PARAMETER SkipBuild
    Não executa a etapa de build.

.PARAMETER SkipTests
    Não executa a etapa de testes.

.PARAMETER LocalAssertions
    Executa a etapa de testes pelo entry point local da assembly, em vez de
    dotnet test. Compila o projeto de testes antes, para a assembly existir, e
    então executa `dotnet <assembly de testes>.dll`. Requer a etapa de testes
    habilitada (ou seja, não usar junto de -SkipTests). O caminho padrão
    (dotnet test) permanece o mesmo quando esta opção não é informada.

.EXAMPLE
    pwsh -File scripts/Build.ps1

.EXAMPLE
    pwsh -File scripts/Build.ps1 -Configuration Release -SkipTests

.EXAMPLE
    pwsh -File scripts/Build.ps1 -LocalAssertions -SkipBuild

.OUTPUTS
    0 quando todas as etapas executadas passam. 1 em caso de falha ou de
    pré-requisito ausente (por exemplo, o SDK do .NET fora do PATH).

.NOTES
    Este script executa dotnet build e dotnet test; ele não abre a engine. Com
    -LocalAssertions, o segundo comando é `dotnet <assembly de testes>.dll`.

    Se o PowerShell recusar a execução do arquivo por política de execução, use
    um bypass válido apenas para aquele processo:

        powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1

    O bypass vale só para esse processo e não altera configuração global.
#>

[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [switch] $SkipBuild,

    [switch] $SkipTests,

    [switch] $LocalAssertions
)

$ErrorActionPreference = 'Stop'

# Uma falha do dotnet deve ser reportada pelo código de saída, não como
# exceção, para que o resumo final sempre apareça.
if (Test-Path -LiteralPath 'variable:PSNativeCommandUseErrorActionPreference') {
    $PSNativeCommandUseErrorActionPreference = $false
}

# --- Caminhos resolvidos a partir da pasta deste script -----------------------
$repoRoot     = Split-Path -Parent $PSScriptRoot
$godotProject = Join-Path $repoRoot 'src\IndexCities.Godot\IndexCities.Godot.csproj'
$testProject  = Join-Path $repoRoot 'tests\IndexCities.Simulation.Tests\IndexCities.Simulation.Tests.csproj'

function Write-Step {
    param(
        [string] $Message,
        [ConsoleColor] $Color = 'Cyan'
    )

    Write-Host ''
    Write-Host "==> $Message" -ForegroundColor $Color
}

function Invoke-DotNet {
    param(
        [string]   $Title,
        [string[]] $Arguments
    )

    Write-Step $Title
    Write-Host "    dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray

    # Out-Host mantém a saída do dotnet fora do fluxo de saída da função, para
    # que o valor de retorno seja apenas o código de saída.
    & $script:DotNetExe @Arguments | Out-Host
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host "    OK: $Title" -ForegroundColor Green
    }
    else {
        Write-Host "    FALHOU (código $exitCode): $Title" -ForegroundColor Red
    }

    return $exitCode
}

function Resolve-TestAssemblyPath {
    param(
        [string] $ProjectPath,
        [string] $BuildConfiguration
    )

    $projectDir  = Split-Path -Parent $ProjectPath
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    $searchDir   = Join-Path $projectDir (Join-Path 'bin' $BuildConfiguration)

    if (-not (Test-Path -LiteralPath $searchDir -PathType Container)) {
        Write-Host "ERRO: pasta de saída do projeto de testes não encontrada: $searchDir" -ForegroundColor Red
        return $null
    }

    # O TFM (por exemplo, net8.0) fica em uma subpasta do bin; procurar aqui
    # evita repetir no script uma configuração que pertence ao .csproj.
    $found = @(Get-ChildItem -LiteralPath $searchDir -Filter "$projectName.dll" -File -Recurse -ErrorAction SilentlyContinue |
        Sort-Object -Property LastWriteTimeUtc -Descending)

    if ($found.Count -eq 0) {
        Write-Host "ERRO: $projectName.dll não foi encontrado em: $searchDir" -ForegroundColor Red
        return $null
    }

    return $found[0].FullName
}

# --- Pré-requisitos ----------------------------------------------------------
$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
if (-not $dotnetCommand) {
    Write-Host 'ERRO: o comando "dotnet" não foi encontrado no PATH.' -ForegroundColor Red
    Write-Host '      Instale o SDK do .NET (8.0 ou superior) e reabra o terminal.' -ForegroundColor Red
    exit 1
}
$script:DotNetExe = $dotnetCommand.Source

# --- Etapas ------------------------------------------------------------------
$results = [ordered]@{}
$notRun  = @()

if ($LocalAssertions -and $SkipTests) {
    Write-Host 'Aviso: -LocalAssertions não tem efeito com -SkipTests.' -ForegroundColor Yellow
}

if (-not $SkipBuild) {
    if (-not (Test-Path -LiteralPath $godotProject -PathType Leaf)) {
        Write-Host "ERRO: projeto não encontrado: $godotProject" -ForegroundColor Red
        exit 1
    }

    $results['build src/IndexCities.Godot'] = Invoke-DotNet `
        -Title 'Build do projeto Godot' `
        -Arguments @('build', $godotProject, '-c', $Configuration)
}

if (-not $SkipTests) {
    if (-not (Test-Path -LiteralPath $testProject -PathType Leaf)) {
        Write-Host "ERRO: projeto de testes não encontrado: $testProject" -ForegroundColor Red
        exit 1
    }

    if ($LocalAssertions) {
        # Caminho alternativo explícito: o runner VSTest falha neste ambiente ao
        # abrir o processo pai. As mesmas assertions são executadas pelo entry
        # point local da assembly de testes, que precisa ser compilada antes.
        $results['build tests/IndexCities.Simulation.Tests'] = Invoke-DotNet `
            -Title 'Build do projeto de testes (assertions locais)' `
            -Arguments @('build', $testProject, '-c', $Configuration)

        if ($results['build tests/IndexCities.Simulation.Tests'] -eq 0) {
            $testAssembly = Resolve-TestAssemblyPath -ProjectPath $testProject -BuildConfiguration $Configuration

            if ($testAssembly) {
                Write-Host "    Assembly: $testAssembly" -ForegroundColor DarkGray

                $results['assertions locais tests/IndexCities.Simulation.Tests'] = Invoke-DotNet `
                    -Title 'Assertions locais da simulação' `
                    -Arguments @($testAssembly)
            }
            else {
                $notRun += 'assertions locais tests/IndexCities.Simulation.Tests (assembly não encontrada)'
            }
        }
        else {
            Write-Host '    As assertions locais não serão executadas sobre uma assembly antiga.' -ForegroundColor Yellow
            $notRun += 'assertions locais tests/IndexCities.Simulation.Tests (build do projeto de testes falhou)'
        }
    }
    else {
        $results['test tests/IndexCities.Simulation.Tests'] = Invoke-DotNet `
            -Title 'Testes da simulação' `
            -Arguments @('test', $testProject, '-c', $Configuration)
    }
}

# --- Resumo ------------------------------------------------------------------
Write-Step 'Resumo'

if ($results.Count -eq 0 -and $notRun.Count -eq 0) {
    Write-Host '    Nenhuma etapa foi executada (-SkipBuild e -SkipTests).' -ForegroundColor Yellow
    exit 0
}

$failed = @()
foreach ($name in $results.Keys) {
    $exitCode = $results[$name]

    if ($exitCode -eq 0) {
        Write-Host ('    [ok]     {0}' -f $name) -ForegroundColor Green
    }
    else {
        Write-Host ('    [falhou] {0} (código {1})' -f $name, $exitCode) -ForegroundColor Red
        $failed += $name
    }
}

foreach ($name in $notRun) {
    Write-Host ('    [não executado] {0}' -f $name) -ForegroundColor Red
    $failed += $name
}

Write-Host ''

if ($failed.Count -gt 0) {
    Write-Host ('Falha em: {0}' -f ($failed -join '; ')) -ForegroundColor Red
    exit 1
}

Write-Host 'Todas as etapas passaram.' -ForegroundColor Green
exit 0
