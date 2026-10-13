<#
.SYNOPSIS
    Verifica se os links locais dos arquivos Markdown do repositório existem.

.DESCRIPTION
    Percorre todos os arquivos *.md do repositório e confere o destino de links
    e imagens que apontam para caminhos locais.

    Destinos ignorados (não são considerados links quebrados):

        - URLs e esquemas externos, como http://, https://, mailto: e res://;
        - URLs relativas ao protocolo, começando por //;
        - fragmentos/âncoras (#...) e query strings (?...);
        - trechos dentro de blocos de código cercados por ``` ou ~~~ e trechos
          entre crases, para não tratar exemplos como links reais.

    Pastas ignoradas:

        - bin, obj, .godot e .git em qualquer nível;
        - src\IndexCities.Godot\Assets\V1\Source.

    Arquivos nessas pastas não são analisados, e um destino dentro delas é
    aceito sem verificação. Um destino é considerado válido quando o arquivo ou
    a pasta existe; para links sem extensão também são aceitas as resoluções
    usuais do GitHub: <alvo>.md e <alvo>/README.md.

    O script é somente leitura: não cria, altera nem apaga arquivos.

.PARAMETER RepoRoot
    Raiz do repositório. Padrão: a pasta pai de $PSScriptRoot.

.EXAMPLE
    pwsh -File scripts/Validate-Docs.ps1

.OUTPUTS
    0 quando todos os links locais resolvem. 1 quando há links quebrados ou
    quando a raiz do repositório não existe.

.NOTES
    Este script é somente leitura e não abre a engine.

    Se o PowerShell recusar a execução do arquivo por política de execução, use
    um bypass válido apenas para aquele processo:

        powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Validate-Docs.ps1

    O bypass vale só para esse processo e não altera configuração global.
#>

[CmdletBinding()]
param(
    [string] $RepoRoot
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

# Pastas ignoradas pelo nome do diretório, em qualquer nível.
$excludedDirectoryNames = @('bin', 'obj', '.godot', '.git')

# Pastas ignoradas por caminho relativo à raiz do repositório.
$excludedRelativePaths = @(
    'src\IndexCities.Godot\Assets\V1\Source'
)

# --- Utilidades --------------------------------------------------------------

function Get-RepositoryRelativePath {
    param(
        [string] $BasePath,
        [string] $FullPath
    )

    $base = $BasePath.TrimEnd('\', '/')

    if ($FullPath.Length -le $base.Length) { return '' }

    return $FullPath.Substring($base.Length).TrimStart([char[]]@('\', '/'))
}

function Test-PathIsExcluded {
    param([string] $FullPath)

    $normalized = $FullPath
    try { $normalized = [System.IO.Path]::GetFullPath($FullPath) } catch { }

    $relative = Get-RepositoryRelativePath -BasePath $script:RepoRootFull -FullPath $normalized
    if (-not $relative) { return $false }

    $relative = $relative -replace '/', '\'

    foreach ($segment in ($relative -split '\\')) {
        if ($excludedDirectoryNames -contains $segment) { return $true }
    }

    foreach ($excluded in $excludedRelativePaths) {
        $prefix = $excluded.TrimEnd('\', '/')
        if ($relative -like "$prefix\*") { return $true }
    }

    return $false
}

function ConvertTo-LocalLinkPath {
    param([string] $Target)

    $value = $Target.Trim()

    if ($value.StartsWith('<') -and $value.EndsWith('>')) {
        $value = $value.Substring(1, $value.Length - 2).Trim()
    }

    if (-not $value) { return $null }                                  # destino vazio
    if ($value.StartsWith('#')) { return $null }                       # âncora no próprio arquivo
    if ($value.StartsWith('//')) { return $null }                      # URL relativa ao protocolo
    if ($value -match '^[a-zA-Z][a-zA-Z0-9+.\-]*:') { return $null }   # esquema: http, https, mailto, res, ...

    # Fragmento e query string não fazem parte do caminho do arquivo.
    $value = ($value -split '#', 2)[0]
    $value = ($value -split '\?', 2)[0]
    if (-not $value) { return $null }

    try { $value = [System.Uri]::UnescapeDataString($value) } catch { }

    return $value
}

function Test-LinkTargetExists {
    param(
        [string] $LocalPath,
        [string] $SourceDirectory
    )

    $relative = $LocalPath -replace '/', '\'

    try {
        if ($relative.StartsWith('\')) {
            $base = Join-Path $script:RepoRootFull $relative.TrimStart('\')
        }
        else {
            $base = Join-Path $SourceDirectory $relative
        }
    }
    catch {
        return $false
    }

    $candidates = @($base)

    try { $hasExtension = [System.IO.Path]::HasExtension($base) }
    catch { $hasExtension = $true }

    if (-not $hasExtension) {
        $candidates += ($base + '.md')
        $candidates += (Join-Path $base 'README.md')
    }

    foreach ($candidate in $candidates) {
        if (Test-PathIsExcluded -FullPath $candidate) { return $true }
        if (Test-Path -LiteralPath $candidate) { return $true }
    }

    return $false
}

function Get-MarkdownLinkTargets {
    param([string[]] $Lines)

    $targets   = @()
    $inFence   = $false
    $fenceChar = ''

    for ($index = 0; $index -lt $Lines.Count; $index++) {
        $line       = $Lines[$index]
        $lineNumber = $index + 1

        $fence = [regex]::Match($line, '^\s{0,3}(?<marker>`{3,}|~{3,})')
        if ($fence.Success) {
            $marker = $fence.Groups['marker'].Value.Substring(0, 1)

            if (-not $inFence) {
                $inFence   = $true
                $fenceChar = $marker
            }
            elseif ($marker -eq $fenceChar) {
                $inFence   = $false
                $fenceChar = ''
            }

            continue
        }

        if ($inFence) { continue }

        # Remove trechos entre crases para não ler exemplos como links reais.
        $scan = [regex]::Replace($line, '``[^`]*``', '')
        $scan = [regex]::Replace($scan, '`[^`]*`', '')

        foreach ($match in [regex]::Matches($scan, '!?\[[^\]]*\]\(\s*(?<target><[^>]*>|[^)\s]+)')) {
            $targets += [pscustomobject]@{ Line = $lineNumber; Target = $match.Groups['target'].Value }
        }

        $definition = [regex]::Match($scan, '^\s{0,3}\[[^\]]+\]:\s*(?<target><[^>]*>|\S+)')
        if ($definition.Success) {
            $targets += [pscustomobject]@{ Line = $lineNumber; Target = $definition.Groups['target'].Value }
        }
    }

    return $targets
}

# --- Raiz do repositório -----------------------------------------------------
$resolvedRoot = Resolve-Path -LiteralPath $RepoRoot -ErrorAction SilentlyContinue
if (-not $resolvedRoot) {
    Write-Host "ERRO: a raiz do repositório não existe: $RepoRoot" -ForegroundColor Red
    Write-Host '      Use -RepoRoot para informar a pasta correta.' -ForegroundColor Red
    exit 1
}

$script:RepoRootFull = $resolvedRoot.ProviderPath

# --- Coleta dos arquivos Markdown -------------------------------------------
$markdownFiles = @(
    Get-ChildItem -LiteralPath $script:RepoRootFull -Filter '*.md' -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { -not (Test-PathIsExcluded -FullPath $_.FullName) } |
        Sort-Object -Property FullName
)

Write-Host 'Verificando links locais em Markdown...' -ForegroundColor Cyan
Write-Host "Raiz:      $script:RepoRootFull" -ForegroundColor DarkGray
Write-Host ("Ignoradas: {0}, {1}" -f ($excludedDirectoryNames -join ', '), ($excludedRelativePaths -join ', ')) -ForegroundColor DarkGray

if ($markdownFiles.Count -eq 0) {
    Write-Host ''
    Write-Host 'Nenhum arquivo Markdown encontrado fora das pastas ignoradas.' -ForegroundColor Yellow
    exit 0
}

# --- Verificação -------------------------------------------------------------
$checkedLinks = 0
$brokenLinks  = @()

foreach ($file in $markdownFiles) {
    $lines = @(Get-Content -LiteralPath $file.FullName -Encoding utf8)
    if ($lines.Count -eq 0) { continue }

    $sourceDirectory = Split-Path -Parent $file.FullName
    $relativeSource  = Get-RepositoryRelativePath -BasePath $script:RepoRootFull -FullPath $file.FullName

    foreach ($link in (Get-MarkdownLinkTargets -Lines $lines)) {
        $localPath = ConvertTo-LocalLinkPath -Target $link.Target
        if (-not $localPath) { continue }

        $checkedLinks++

        if (-not (Test-LinkTargetExists -LocalPath $localPath -SourceDirectory $sourceDirectory)) {
            $brokenLinks += [pscustomobject]@{
                Source = $relativeSource
                Line   = $link.Line
                Target = $link.Target
            }
        }
    }
}

# --- Relatório ---------------------------------------------------------------
if ($brokenLinks.Count -gt 0) {
    Write-Host ''
    foreach ($broken in $brokenLinks) {
        Write-Host ("{0}:{1} -> {2}" -f $broken.Source, $broken.Line, $broken.Target) -ForegroundColor Red
    }
}

Write-Host ''
Write-Host ("Resumo: {0} arquivo(s) Markdown, {1} link(s) local(is) verificado(s), {2} quebrado(s)." -f `
    $markdownFiles.Count, $checkedLinks, $brokenLinks.Count) -ForegroundColor Cyan

if ($brokenLinks.Count -gt 0) {
    Write-Host 'Há links locais quebrados.' -ForegroundColor Red
    exit 1
}

Write-Host 'Todos os links locais resolvem.' -ForegroundColor Green
exit 0
